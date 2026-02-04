# Unified Billing Architecture: Tiny Tools + AI PatchNotes

## Current State

### PatchNotes (mypkgnotes.com)
- **Auth**: Stytch Consumer SDK (magic links, individual users)
- **Session**: Cookie-based (`stytch_session`), validated server-side via Stytch API
- **Stripe**: Hosted Checkout, Customer linked to individual `User` via `stytch_user_id` in metadata
- **User model**: Flat — `User` has `StytchUserId`, `StripeCustomerId`, `SubscriptionStatus`
- **Billing entity**: Individual user
- **Webhook filtering**: Uses `app: "patchnotes"` metadata (only present on checkout session, not on subsequent subscription events)
- **Backend**: .NET minimal API, SQLite
- **Key files**:
  - `PatchNotes.Api/Routes/SubscriptionRoutes.cs` — checkout, portal, status endpoints
  - `PatchNotes.Api/Webhooks/StripeWebhook.cs` — webhook handler
  - `PatchNotes.Data/User.cs` — user model with Stripe fields
  - `patchnotes-web/src/stores/subscriptionStore.ts` — frontend subscription state

### Tiny Tools (project.usetinytools.com)
- **Auth**: Stytch B2B SDK (organization discovery, multi-org support)
- **Session**: JWT-based (Bearer token), validated via Stytch JWKS endpoint
- **Stripe**: Embedded/custom checkout (`uiMode: "custom"`), 14-day trial
- **User model**: `User` -> `UserOrganization` -> `Organization` (Organization has Stripe fields)
- **Billing entity**: Organization (company pays, not individual)
- **Seat tracking**: `MaxUsers` field exists on Organization but not enforced yet
- **Backend**: .NET minimal API
- **Key files**:
  - `api-yourtinytools-com/Routes/StripeRoutes.cs` — checkout, subscription status, product listing
  - `api-yourtinytools-com/Routes/WebhookRoutes.cs` — Stytch + Stripe webhook handlers
  - `api-yourtinytools-com/Data/ToolsContext.cs` — all models including Organization with Stripe fields

## Design Decisions

### Keep two Stytch projects
PatchNotes uses Consumer auth (individual users), Tiny Tools uses B2B auth (organization-scoped members). These are fundamentally different SDKs and flows. Merging them would add complexity with no user-facing benefit.

### Keep separate databases
Each app tracks its own subscription state independently:
- PatchNotes: "Is this user a Pro subscriber?" — `SubscriptionStatus` on `User`
- Tiny Tools: "What plan is this org on?" — `SubscriptionStatus` on `Organization`

No shared subscription state between the apps. No schema changes needed.

### Share the Stripe Customer (by email)
One Stripe Customer per human, matched by email. Both apps look up existing customers before creating checkout sessions. This ensures the Stripe Customer Portal shows all subscriptions across both apps in one place.

### Stripe Customer Portal for unified subscription management
Both apps link to the same Stripe Customer Portal. Since both products' subscriptions are on the same Stripe Customer, the portal handles the unified view out of the box. No custom dashboard needed.

## Architecture

### Shared Stripe Customer Lookup

Before creating a checkout session, both apps must:
1. Call `Customers.List(email)` in the Stripe API
2. If a customer exists, pass `customer` (not `customerEmail`) to the checkout session
3. If not, let Stripe create one (or create explicitly)

This prevents duplicate Stripe Customers when the same person uses both apps.

### Webhook Router (Azure Function)

Single Stripe account = single webhook endpoint URL. A lightweight Azure Function acts as a router:

```
Stripe --> Azure Function (webhook router)
              |
              |-- inspects product/price ID on the subscription
              |
              |--> forwards to PatchNotes API internal webhook endpoint
              |--> forwards to Tiny Tools API internal webhook endpoint
```

**Router logic:**
1. Verify Stripe signature
2. Extract subscription from event payload
3. Look up product/price ID on the subscription items
4. Match product to app (maintain a simple product-to-app mapping)
5. Forward the raw event to the correct app's internal webhook URL
6. If product not recognized, log and drop

**Each app's webhook handler:**
- No longer registered directly with Stripe
- Receives forwarded events from the router
- Needs its own verification mechanism (shared secret or internal network trust)
- Processes events exactly as it does today

**Benefits:**
- Apps remain decoupled — neither knows about the other's database
- Adding a new app/product just means adding a mapping entry in the router
- Each app keeps its existing webhook handler logic

### Event Routing by Product

Events that contain subscription data (most billing events):
- `checkout.session.completed` — has `line_items` with price/product
- `customer.subscription.created/updated/deleted` — has `items` with price/product
- `invoice.payment_failed/succeeded` — has `lines` with price/product

Events that are customer-level (no product):
- `customer.updated`, `customer.deleted`
- Neither app needs these today — drop silently. Can revisit if needed.

## Webhook Router: Azure Service Evaluation

### Options Considered

**Azure Event Grid (Custom Topics)**
Requires a thin receiver (Azure Function) to accept the Stripe webhook, verify the signature, and publish to an Event Grid custom topic. Event Grid subscriptions with subject/attribute filtering could route events to each app. Adds unnecessary indirection — we'd still need a function to receive and publish, so Event Grid becomes a middleman.

**Azure API Management (`send-request` policy)**
APIM can receive the webhook and use multiple `send-request` policies in fire-and-forget mode to forward to both apps, with conditional logic in policy XML. Downsides: APIM policy XML is painful to maintain, and APIM is expensive for just this use case.

**Azure Function (recommended)**
A single HTTP-triggered function on a consumption plan that verifies the Stripe signature, inspects the product/price, and forwards to the correct app. Minimal infrastructure, easy to debug, cheap, and we already deploy to Azure.

### Decision: Azure Function on Consumption Plan

For a simple content-based router with two destinations, Event Grid and APIM are overkill. An Azure Function scales trivially — adding more apps is just another entry in the routing config.

### Router Configuration

The router uses a JSON config file to map Stripe product IDs to app webhook endpoints. This keeps the routing logic data-driven and avoids code changes when products or URLs change.

```json
{
  "routes": [
    {
      "app": "patchnotes",
      "products": ["prod_XXXXX"],
      "prices": ["price_1Ssnqu7K9OncE5S1tBMkoNg5"],
      "webhookUrl": "https://api.mypkgnotes.com/webhooks/stripe-internal"
    },
    {
      "app": "tinytools",
      "products": ["prod_YYYYY", "prod_ZZZZZ"],
      "prices": [],
      "webhookUrl": "https://api.yourtinytools.com/webhook/stripe-internal"
    }
  ]
}
```

**Routing logic:**
1. Verify Stripe signature
2. Parse event, extract product/price IDs from subscription items (or line items, or invoice lines depending on event type)
3. Look up matching route in config by product ID, fall back to price ID
4. Forward raw event body + a shared secret header to the matched app's `webhookUrl`
5. If no route matches (including customer-level events with no product context): log and return 200 to Stripe (don't cause retries). Neither app reacts to customer-level events today — payment method and email changes are handled within Stripe/Customer Portal. We can add forwarding for these later if needed.

**Config can be stored as:**
- A JSON file deployed with the function (simplest, update via redeploy)
- An Azure App Configuration resource (if we want runtime updates without redeploy)
- A blob in Azure Storage (middle ground — update the file, function picks it up)

Starting with a deployed JSON file is fine. We can move to App Configuration later if the routing changes frequently.

## Changes Required

### New: Webhook Router (Azure Function)
- Stripe signature verification
- Product-to-app URL mapping (config-driven)
- Forward events to correct app endpoint
- Handle customer-level events (forward to both)

### PatchNotes Changes
- **Checkout flow**: Look up existing Stripe Customer by email before creating session
- **Webhook**: Switch from metadata-based filtering (`app: "patchnotes"`) to product/price-based filtering
- **Webhook**: Accept forwarded events from router (adjust signature verification or add internal auth)

### Tiny Tools Changes
- **Checkout flow**: Look up existing Stripe Customer by email before creating session
- **Webhook**: Accept forwarded events from router (same as above)

### Stripe Dashboard
- Register the Azure Function URL as the webhook endpoint
- Subscribe to: `checkout.session.completed`, `customer.subscription.*`, `invoice.*`

## Edge Cases

### Same person, different emails across apps
They get two Stripe Customers and two separate portal views. Acceptable — if they use the same email, it unifies automatically.

### Organization admin in Tiny Tools is also a PatchNotes user
Works fine. The Organization's `StripeCustomerId` and the User's `StripeCustomerId` may differ (org email vs personal email). Each app manages its own customer relationship independently. The portal only unifies when the same email is used.

### Webhook delivery failures
The router should return 200 to Stripe as long as it successfully received the event. If forwarding to an app fails, the router should retry or queue the event. Consider using Azure Queue Storage for reliability.
