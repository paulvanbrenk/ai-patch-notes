# PatchNotes.Api

ASP.NET Core Web API for the PatchNotes application.

## Overview

This project provides the REST API for managing packages, releases, notifications, and user authentication via Stytch.

## Running

```bash
dotnet run
```

The API runs on `http://localhost:5031` by default.

## API Endpoints

### Packages

| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| GET | `/api/packages` | List all tracked packages | No |
| GET | `/api/packages/{id}` | Get package details | No |
| POST | `/api/packages` | Add a new package | Yes |
| PATCH | `/api/packages/{id}` | Update package GitHub mapping | Yes |
| DELETE | `/api/packages/{id}` | Remove a package | Yes |
| GET | `/api/packages/{id}/releases` | Get releases for a package | No |

### Releases

| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| GET | `/api/releases` | Query releases (supports `packages` and `days` params) | No |
| GET | `/api/releases/{id}` | Get release details | No |
| POST | `/api/releases/{id}/summarize` | Generate AI summary (supports SSE streaming) | Yes |

### Notifications

| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| GET | `/api/notifications` | Query notifications | Yes |
| GET | `/api/notifications/unread-count` | Get unread count | Yes |
| PATCH | `/api/notifications/{id}/read` | Mark as read | Yes |
| DELETE | `/api/notifications/{id}` | Delete notification | Yes |

### Users

| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| GET | `/api/users/me` | Get current authenticated user | Yes |
| POST | `/api/users/login` | Create/update user on login | Yes |

### Webhooks

| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| POST | `/webhooks/stytch` | Handle Stytch webhook events (Svix signature verified) | No |

### Watchlist

| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| GET | `/api/watchlist` | Get current user's watched package IDs | Yes |
| PUT | `/api/watchlist` | Replace entire watchlist (bulk set) | Yes |
| POST | `/api/watchlist/{packageId}` | Add a package to watchlist | Yes |
| DELETE | `/api/watchlist/{packageId}` | Remove a package from watchlist | Yes |

### Subscriptions

| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| POST | `/api/subscription/checkout` | Create Stripe Checkout session | Yes |
| POST | `/api/subscription/portal` | Create Stripe Customer Portal session | Yes |
| GET | `/api/subscription/status` | Get current subscription status | Yes |

## Configuration

Configuration is loaded from `appsettings.json` and environment variables:

- `AI:ApiKey` - API key for AI summarization (Groq)
- `AI:BaseUrl` - Optional custom AI API base URL
- `AI:Model` - Optional model override
- `Stytch:ProjectId` - Stytch project ID
- `Stytch:Secret` - Stytch secret
- `Stytch:WebhookSecret` - Secret for webhook signature verification

## Dependencies

- **PatchNotes.Data** - Data layer and external clients
