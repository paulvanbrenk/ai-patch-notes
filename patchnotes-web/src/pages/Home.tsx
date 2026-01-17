import { Header, HeaderTitle, Container, Button, Input } from '../components/ui'
import { PackageCard, ReleaseCard } from '../components/releases'

const mockPackages = [
  {
    id: 1,
    npmName: 'react',
    githubOwner: 'facebook',
    githubRepo: 'react',
    releaseCount: 127,
    lastFetchedAt: '2026-01-17T10:30:00Z',
  },
  {
    id: 2,
    npmName: 'typescript',
    githubOwner: 'microsoft',
    githubRepo: 'TypeScript',
    releaseCount: 89,
    lastFetchedAt: '2026-01-16T15:45:00Z',
  },
  {
    id: 3,
    npmName: 'vite',
    githubOwner: 'vitejs',
    githubRepo: 'vite',
    releaseCount: 64,
    lastFetchedAt: '2026-01-15T08:20:00Z',
  },
]

const mockReleases = [
  {
    id: 1,
    tag: 'v19.0.0',
    title: 'React 19',
    body: 'This major release includes Actions, new hooks like useActionState and useOptimistic, and significant improvements to ref handling.',
    publishedAt: '2026-01-10T14:00:00Z',
    htmlUrl: 'https://github.com/facebook/react/releases/tag/v19.0.0',
  },
  {
    id: 2,
    tag: 'v18.3.1',
    title: null,
    body: 'Bug fix release addressing hydration issues in concurrent mode.',
    publishedAt: '2026-01-05T09:30:00Z',
    htmlUrl: 'https://github.com/facebook/react/releases/tag/v18.3.1',
  },
  {
    id: 3,
    tag: 'v19.0.0-rc.1',
    title: 'React 19 RC 1',
    body: 'Release candidate for React 19 with all planned features.',
    publishedAt: '2025-12-20T16:00:00Z',
    htmlUrl: 'https://github.com/facebook/react/releases/tag/v19.0.0-rc.1',
  },
  {
    id: 4,
    tag: 'v18.3.0',
    title: 'React 18.3',
    body: 'Minor release with new deprecation warnings for features changing in React 19.',
    publishedAt: '2025-12-01T11:00:00Z',
    htmlUrl: 'https://github.com/facebook/react/releases/tag/v18.3.0',
  },
]

export function Home() {
  return (
    <div className="min-h-screen bg-surface-secondary">
      <Header>
        <HeaderTitle>Patch Notes</HeaderTitle>
        <div className="flex items-center gap-3">
          <Button variant="secondary" size="sm">
            Settings
          </Button>
          <Button size="sm">Add Package</Button>
        </div>
      </Header>

      <main className="py-8">
        <Container>
          {/* Search */}
          <div className="mb-8">
            <Input
              placeholder="Search packages or releases..."
              className="max-w-md"
            />
          </div>

          {/* Packages Section */}
          <section className="mb-12">
            <h2 className="text-xl font-semibold text-text-primary mb-4">
              Tracked Packages
            </h2>
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
              {mockPackages.map((pkg) => (
                <PackageCard
                  key={pkg.id}
                  npmName={pkg.npmName}
                  githubOwner={pkg.githubOwner}
                  githubRepo={pkg.githubRepo}
                  releaseCount={pkg.releaseCount}
                  lastFetchedAt={pkg.lastFetchedAt}
                  onClick={() => console.log(`Clicked ${pkg.npmName}`)}
                />
              ))}
            </div>
          </section>

          {/* Recent Releases Section */}
          <section>
            <h2 className="text-xl font-semibold text-text-primary mb-4">
              Recent Releases
            </h2>
            <div className="space-y-4">
              {mockReleases.map((release) => (
                <ReleaseCard
                  key={release.id}
                  tag={release.tag}
                  title={release.title}
                  body={release.body}
                  publishedAt={release.publishedAt}
                  htmlUrl={release.htmlUrl}
                />
              ))}
            </div>
          </section>
        </Container>
      </main>
    </div>
  )
}
