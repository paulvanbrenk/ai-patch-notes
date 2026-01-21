# Stytch B2C Authentication Setup

This document outlines the steps to complete the Stytch B2C authentication setup.

## What's Been Implemented

### Frontend

- Stytch React SDK integration (`@stytch/react`, `@stytch/vanilla-js`)
- `StytchProvider` wrapper in `main.tsx`
- Login page at `/login` with pre-built UI (Email Magic Links + Google OAuth)
- Authentication callback handler at `/authenticate`
- Stytch client configuration in `src/auth/stytch.ts`
- `UserMenu` component in header for login/logout

### Backend

- `Users` table in database (via EF Core migration)
- `POST /api/users/login` - Create/update user on login
- `GET /api/users/me?stytchUserId=xxx` - Get user by Stytch ID
- `POST /api/webhooks/stytch` - Handle Stytch webhook events (user.created, user.updated, user.deleted)
- **Stytch session authentication** - Protected API endpoints now validate Stytch sessions (replaced API key auth)
- `IStytchClient` service for validating sessions via Stytch API

## Next Steps for You

### 1. Create a Stytch Account & Project

1. Go to [https://stytch.com](https://stytch.com) and create an account
2. Create a new **Consumer** project (B2C)
3. Navigate to **API Keys** in the dashboard

### 2. Configure Environment Variables

Update `.env.development` with your actual Stytch public token:

```bash
VITE_STYTCH_PUBLIC_TOKEN=public-token-test-xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx
```

For production, create `.env.production` with your live token:

```bash
VITE_STYTCH_PUBLIC_TOKEN=public-token-live-xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx
```

### 3. Enable Frontend SDKs in Stytch Dashboard

1. Go to **Dashboard** → **Configuration** → **SDK Configuration**
2. Enable **Frontend SDKs** for your environment (Test/Live)
3. Add your application domain to **Authorized applications**:
   - For local development: `http://localhost:5173`
   - For production: your production domain

### 4. Configure Redirect URLs

1. Go to **Dashboard** → **Redirect URLs**
2. Add the following URLs:
   - **Login**: `http://localhost:5173/authenticate` (dev)
   - **Signup**: `http://localhost:5173/authenticate` (dev)
   - Add production URLs when deploying

### 5. Enable Authentication Methods

#### Email Magic Links

1. Go to **Dashboard** → **Authentication** → **Email Magic Links**
2. Enable Email Magic Links
3. Configure email templates if desired

#### Google OAuth (optional)

1. Go to **Dashboard** → **Authentication** → **OAuth**
2. Enable Google OAuth
3. Configure your Google OAuth credentials:
   - Create a project in [Google Cloud Console](https://console.cloud.google.com)
   - Enable the Google+ API
   - Create OAuth 2.0 credentials
   - Add `https://test.stytch.com/v1/oauth/callback` as an authorized redirect URI
   - Copy Client ID and Client Secret to Stytch dashboard

### 6. Install Dependencies

```bash
cd patchnotes-web
npm install
# or
pnpm install
```

### 7. Configure Backend Stytch Credentials

The backend needs Stytch credentials to validate session tokens.

1. Go to **Dashboard** → **API Keys**
2. Copy your **Project ID** and **Secret** (use Test credentials for development)
3. Add to your backend configuration:

```bash
# In appsettings.Development.json or environment variables
Stytch__ProjectId=project-test-xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx
Stytch__Secret=secret-test-xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx
```

Or in `appsettings.Development.json`:

```json
{
  "Stytch": {
    "ProjectId": "project-test-xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
    "Secret": "secret-test-xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx"
  }
}
```

For production, use `Stytch__BaseUrl=https://api.stytch.com` (default is test environment).

### 8. Configure Webhooks (Optional - Backend User Sync)

Webhooks allow Stytch to notify your backend when users are created, updated, or deleted.

1. Go to **Dashboard** → **Webhooks**
2. Click **Add webhook endpoint**
3. Configure the endpoint:
   - **URL**: `https://your-api-domain.com/api/webhooks/stytch`
   - For local development, use a tunnel like [ngrok](https://ngrok.com): `ngrok http 5000`
   - Then use the ngrok URL: `https://xxxx.ngrok.io/api/webhooks/stytch`
4. Select events to subscribe to:
   - `user.created`
   - `user.updated`
   - `user.deleted`
5. Copy the **Webhook signing secret** and add it to your backend configuration:

```bash
# In appsettings.Development.json or environment variables
Stytch__WebhookSecret=whsec_xxxxxxxxxxxxxxxxxxxxxxxx
```

### 9. Run Database Migration

Apply the Users table migration:

```bash
cd PatchNotes.Api
dotnet ef database update --project ../PatchNotes.Data
```

Or if running with SQLite, the migration will apply automatically on startup.

### 10. Test the Integration

1. Start the backend API: `cd PatchNotes.Api && dotnet run`
2. Start the frontend dev server: `cd patchnotes-web && npm run dev`
3. Navigate to `http://localhost:5173/login`
4. Test Email Magic Link login
5. Check the browser console for any errors
6. Verify user was created in database

## Optional Enhancements

### Sync User on Login (Alternative to Webhooks)

For simpler setups or local development without ngrok, you can sync users from the frontend on successful login:

```tsx
// In your Authenticate.tsx or after successful auth
import { useStytchUser } from '@stytch/react'
import { useEffect } from 'react'

function useSyncUser() {
  const { user } = useStytchUser()

  useEffect(() => {
    if (user) {
      fetch('/api/users/login', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          stytchUserId: user.user_id,
          email: user.emails?.[0]?.email,
          name: user.name?.first_name
            ? `${user.name.first_name} ${user.name.last_name || ''}`.trim()
            : null,
        }),
      })
    }
  }, [user])
}
```

### Add Protected Routes

Create an auth guard component to protect routes:

```tsx
// src/auth/AuthGuard.tsx
import { useStytchUser } from '@stytch/react'
import { useNavigate } from '@tanstack/react-router'
import { useEffect } from 'react'

export function AuthGuard({ children }: { children: React.ReactNode }) {
  const { user, isInitialized } = useStytchUser()
  const navigate = useNavigate()

  useEffect(() => {
    if (isInitialized && !user) {
      navigate({ to: '/login' })
    }
  }, [user, isInitialized, navigate])

  if (!isInitialized) return <div>Loading...</div>
  if (!user) return null

  return <>{children}</>
}
```

### Add Logout Functionality

```tsx
import { useStytch } from '@stytch/react'

function LogoutButton() {
  const stytch = useStytch()

  const handleLogout = () => {
    stytch.session.revoke()
  }

  return <button onClick={handleLogout}>Logout</button>
}
```

### Access User Information

```tsx
import { useStytchUser } from '@stytch/react'

function UserProfile() {
  const { user } = useStytchUser()

  return (
    <div>
      <p>Email: {user?.emails?.[0]?.email}</p>
      <p>User ID: {user?.user_id}</p>
    </div>
  )
}
```

### Customize Login UI Styles

Update `src/auth/stytch.ts` to add custom styles:

```tsx
export const stytchLoginStyles = {
  container: {
    backgroundColor: '#ffffff',
    borderRadius: '8px',
    boxShadow: '0 2px 8px rgba(0, 0, 0, 0.1)',
    padding: '40px',
  },
  colors: {
    primary: '#3b82f6', // Your brand color
  },
  fontFamily: 'Inter, system-ui, sans-serif',
}

// Then use in Login.tsx:
// <StytchLogin config={stytchLoginConfig} styles={stytchLoginStyles} />
```

## Troubleshooting

### "Public token not configured" warning

- Ensure `VITE_STYTCH_PUBLIC_TOKEN` is set in your `.env.development` file
- Restart the dev server after changing environment variables

### "Unauthorized domain" error

- Add your domain to Authorized Applications in Stytch Dashboard
- For localhost, ensure you're using the correct port (5173 for Vite)

### OAuth redirect issues

- Verify redirect URLs are correctly configured in Stytch Dashboard
- Check that Google OAuth credentials are properly set up

## Resources

- [Stytch Documentation](https://stytch.com/docs)
- [Stytch React SDK Reference](https://stytch.com/docs/sdks/javascript-sdk/react)
- [Stytch Dashboard](https://stytch.com/dashboard)
