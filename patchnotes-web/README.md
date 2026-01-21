# patchnotes-web

React frontend for the PatchNotes application.

## Overview

A mobile-first web application for viewing GitHub release notes from tracked npm packages. Features a timeline view, package filtering, and AI-powered release summaries.

## Running

```bash
npm install
npm run dev
```

The app runs on `http://localhost:5173` by default.

## Scripts

| Command | Description |
|---------|-------------|
| `npm run dev` | Start development server |
| `npm run build` | Build for production |
| `npm run preview` | Preview production build |
| `npm test` | Run tests in watch mode |
| `npm run test:run` | Run tests once |
| `npm run test:coverage` | Run tests with coverage |
| `npm run lint` | Run ESLint |
| `npm run format` | Format code with Prettier |
| `npm run format:check` | Check formatting |

## Tech Stack

- **React 19** with TypeScript
- **TanStack Router** - Type-safe file-based routing
- **TanStack Query** - Data fetching and caching
- **TanStack AI** - AI streaming utilities
- **Tailwind CSS 4** - Styling
- **Stytch** - User authentication
- **Vite** - Build tool
- **Vitest** - Testing framework

## Directory Structure

```
src/
├── components/
│   ├── package-picker/    # Package selection UI
│   ├── releases/          # Release timeline components
│   └── ui/                # Shared UI components
├── pages/                 # Route page components
├── api/                   # TanStack Query hooks
├── routes/                # TanStack Router config
└── main.tsx               # App entry point
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
npm test
```
