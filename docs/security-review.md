# Security Review

**Date:** 2026-02-10
**Status:** In Progress

## Resolved

### ~~CRITICAL — Production CORS Allows localhost with Credentials~~

**File:** `infra/main.bicep:78-85` (deleted)

Production Azure App Service allowed credentialed cross-origin requests from `http://localhost:5173` and `http://localhost:3000`. Combined with `supportCredentials: true`, this was exploitable for session hijacking on shared networks.

**Resolution:** Removed unused `infra/` directory entirely.

---

## Open — High Severity

### 1. Stored XSS via Markdown Rendering

**File:** `patchnotes-web/src/pages/ReleaseDetail.tsx:233`

```tsx
<Markdown>{release.body}</Markdown>
```

Release bodies sourced from GitHub are rendered as markdown. A compromised upstream package maintainer could inject malicious HTML/JS into release notes, executing in every user's browser.

**Proposed fix:** Remove inline markdown rendering; open the GitHub release URL in a new window instead.

---

### 2. Unvalidated Redirect URLs (Open Redirect)

**Files:** `patchnotes-web/src/stores/subscriptionStore.ts:53,68`, `PatchNotes.Api/Routes/SubscriptionRoutes.cs:87,121`

```typescript
window.location.href = url  // URL comes straight from API response
```

Checkout and billing portal URLs from the API are redirected to with zero validation. If the backend is compromised or a response is tampered with, users get sent to attacker-controlled domains.

**Proposed fix:** Follow Stripe's recommended pattern — return a server-side `303 See Other` redirect instead of returning the URL as JSON. The frontend would use a plain form submission (`<form method="POST">`) instead of `fetch()` + `window.location.href`. This way the redirect URL never touches client-side JS.

Stripe .NET pattern:
```csharp
Response.Headers.Add("Location", session.Url);
return new StatusCodeResult(303);
```

