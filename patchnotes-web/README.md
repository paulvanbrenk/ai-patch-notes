# patchnotes-web

React frontend for the PatchNotes application.

## Overview

A mobile-first web application for viewing GitHub release notes from tracked npm packages. Features a timeline view, package filtering, AI-powered release summaries, and Stripe-powered subscriptions.

## Running

```bash
pnpm install
pnpm dev
```

The app runs on `http://localhost:5173` by default.

## Scripts

| Command | Description |
|---------|-------------|
| `pnpm dev` | Start development server |
| `pnpm build` | Build for production |
| `pnpm preview` | Preview production build |
| `pnpm test` | Run tests in watch mode |
| `pnpm test:run` | Run tests once |
| `pnpm test:coverage` | Run tests with coverage |
| `pnpm lint` | Run ESLint |
| `pnpm format` | Format code with Prettier |
| `pnpm format:check` | Check formatting |
| `pnpm generate:api` | Regenerate Orval API client from OpenAPI spec |
| `pnpm knip` | Check for unused exports and dependencies |

## Tech Stack

- **React 19** with TypeScript
- **TanStack Router** - Type-safe file-based routing
- **TanStack Query** - Data fetching and caching
- **Tailwind CSS 4** - Styling
- **Stytch** - User authentication
- **Vite 7** - Build tool
- **Vitest** - Testing framework (118 tests)
- **MSW** - API mocking for tests
- **Orval** - OpenAPI client generation
- **Zustand** - Client-side state management
- **react-markdown** - Markdown rendering for summaries
- **Lucide React** - Icons

## Directory Structure

```
src/
├── api/
│   ├── generated/            # Orval-generated API client
│   │   ├── feed/
│   │   ├── model/
│   │   ├── packages/
│   │   ├── releases/
│   │   ├── subscription/
│   │   ├── summaries/
│   │   ├── users/
│   │   └── watchlist/
│   ├── client.ts             # Base API client
│   ├── custom-fetch.ts       # Custom fetch with auth
│   ├── hooks.ts              # Custom React Query hooks
│   └── subscription.ts       # Subscription API
├── auth/
│   └── stytch.ts             # Stytch configuration
├── components/
│   ├── auth/                 # UserMenu
│   ├── landing/              # HeroCard, Logo
│   ├── package-picker/       # PackagePicker
│   ├── releases/             # ReleaseCard, VersionBadge
│   ├── theme/                # ThemeContext, ThemeToggle
│   └── ui/                   # Badge, Button, Card, Checkbox, Container,
│                             # Footer, Header, Input, Modal, Tooltip
├── pages/
│   ├── About.tsx
│   ├── Admin.tsx
│   ├── Authenticate.tsx
│   ├── HomePage.tsx
│   ├── Login.tsx
│   ├── PackageDetail.tsx
│   ├── Pricing.tsx
│   ├── Privacy.tsx
│   ├── ReleaseDetail.tsx
│   └── Settings.tsx
├── routes/                   # TanStack Router file-based routes
├── stores/
│   ├── filterStore.ts        # Filter state (Zustand)
│   └── subscriptionStore.ts  # Subscription state (Zustand)
├── test/
│   ├── mocks/                # MSW handlers
│   ├── setup.ts
│   └── utils.tsx             # Test utilities
└── main.tsx                  # App entry point
```

## Configuration

Create a `.env.local` file:

```bash
VITE_API_URL=http://localhost:5031
VITE_STYTCH_PUBLIC_TOKEN=your-stytch-public-token
```

## Testing

Tests use Vitest with React Testing Library and MSW for API mocking.

```bash
pnpm test
```
