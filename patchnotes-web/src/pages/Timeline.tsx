import { Header, HeaderTitle, Container, Button, Input } from '../components/ui';
import { ReleaseTimeline } from '../components/releases';

// Mock data for timeline - includes package names for cross-package view
const mockTimelineReleases = [
  {
    id: 1,
    tag: 'v19.0.0',
    title: 'React 19',
    body: 'This major release includes Actions, new hooks like useActionState and useOptimistic, and significant improvements to ref handling.',
    publishedAt: '2026-01-10T14:00:00Z',
    htmlUrl: 'https://github.com/facebook/react/releases/tag/v19.0.0',
    packageName: 'react',
  },
  {
    id: 2,
    tag: 'v6.0.7',
    title: 'Vite 6.0.7',
    body: 'Bug fix release with improved HMR reliability and CSS handling fixes.',
    publishedAt: '2026-01-07T10:00:00Z',
    htmlUrl: 'https://github.com/vitejs/vite/releases/tag/v6.0.7',
    packageName: 'vite',
  },
  {
    id: 3,
    tag: 'v18.3.1',
    title: null,
    body: 'Bug fix release addressing hydration issues in concurrent mode.',
    publishedAt: '2026-01-05T09:30:00Z',
    htmlUrl: 'https://github.com/facebook/react/releases/tag/v18.3.1',
    packageName: 'react',
  },
  {
    id: 4,
    tag: 'v5.7.2',
    title: 'TypeScript 5.7.2',
    body: 'Bug fixes including improved type inference and performance optimizations.',
    publishedAt: '2026-01-03T12:00:00Z',
    htmlUrl: 'https://github.com/microsoft/TypeScript/releases/tag/v5.7.2',
    packageName: 'typescript',
  },
  {
    id: 5,
    tag: 'v19.0.0-rc.1',
    title: 'React 19 RC 1',
    body: 'Release candidate for React 19 with all planned features.',
    publishedAt: '2025-12-20T16:00:00Z',
    htmlUrl: 'https://github.com/facebook/react/releases/tag/v19.0.0-rc.1',
    packageName: 'react',
  },
  {
    id: 6,
    tag: 'v4.0.0',
    title: 'Tailwind CSS v4.0',
    body: 'A ground-up rewrite with a new high-performance engine, 10x faster builds, and CSS-first configuration.',
    publishedAt: '2025-12-15T14:00:00Z',
    htmlUrl: 'https://github.com/tailwindlabs/tailwindcss/releases/tag/v4.0.0',
    packageName: 'tailwindcss',
  },
  {
    id: 7,
    tag: 'v15.1.0',
    title: 'Next.js 15.1',
    body: 'The after API is now stable. New forbidden and unauthorized APIs for handling 403 and 401 responses.',
    publishedAt: '2025-12-10T10:00:00Z',
    htmlUrl: 'https://github.com/vercel/next.js/releases/tag/v15.1.0',
    packageName: 'next',
  },
  {
    id: 8,
    tag: 'v18.3.0',
    title: 'React 18.3',
    body: 'Minor release with new deprecation warnings for features changing in React 19.',
    publishedAt: '2025-12-01T11:00:00Z',
    htmlUrl: 'https://github.com/facebook/react/releases/tag/v18.3.0',
    packageName: 'react',
  },
  {
    id: 9,
    tag: 'v6.0.0',
    title: 'Vite 6',
    body: 'New Environment API for better SSR and multi-environment builds. Requires Node.js 20+.',
    publishedAt: '2025-11-26T09:00:00Z',
    htmlUrl: 'https://github.com/vitejs/vite/releases/tag/v6.0.0',
    packageName: 'vite',
  },
  {
    id: 10,
    tag: 'v5.7.0',
    title: 'TypeScript 5.7',
    body: 'Checked imports, path rewriting for relative paths, and V8 compile caching support.',
    publishedAt: '2025-11-22T14:00:00Z',
    htmlUrl: 'https://github.com/microsoft/TypeScript/releases/tag/v5.7.0',
    packageName: 'typescript',
  },
];

export function Timeline() {
  return (
    <div className="min-h-screen bg-surface-secondary">
      <Header>
        <HeaderTitle>Release Timeline</HeaderTitle>
        <div className="flex items-center gap-3">
          <Button variant="secondary" size="sm">
            Filter
          </Button>
          <Button variant="ghost" size="sm">
            Back
          </Button>
        </div>
      </Header>

      <main className="py-8">
        <Container size="md">
          {/* Search/Filter */}
          <div className="mb-8">
            <Input
              placeholder="Filter releases..."
              className="max-w-md"
            />
          </div>

          {/* Stats */}
          <div className="flex items-center gap-6 mb-8 text-sm text-text-secondary">
            <span>
              <strong className="text-text-primary">{mockTimelineReleases.length}</strong> releases
            </span>
            <span>
              <strong className="text-text-primary">5</strong> packages
            </span>
            <span>
              Last 3 months
            </span>
          </div>

          {/* Timeline */}
          <ReleaseTimeline
            releases={mockTimelineReleases}
            showPackageName={true}
          />
        </Container>
      </main>
    </div>
  );
}
