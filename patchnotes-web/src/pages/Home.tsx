import { Header, HeaderTitle, Container, Button, Input } from '../components/ui'
import { PackageCard, ReleaseCard } from '../components/releases'
import { usePackages, useReleases } from '../api/hooks'
import type { Release } from '../api/types'

function getReleaseUrl(release: Release): string {
  const { githubOwner, githubRepo } = release.package
  return `https://github.com/${githubOwner}/${githubRepo}/releases/tag/${release.tag}`
}

export function Home() {
  const { data: packages, isLoading: packagesLoading } = usePackages()
  const { data: releases, isLoading: releasesLoading } = useReleases()
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
              {packagesLoading ? (
                <p className="text-text-secondary">Loading packages...</p>
              ) : packages?.length === 0 ? (
                <p className="text-text-secondary">
                  No packages tracked yet. Add a package to get started.
                </p>
              ) : (
                packages?.map((pkg) => (
                  <PackageCard
                    key={pkg.id}
                    npmName={pkg.npmName}
                    githubOwner={pkg.githubOwner}
                    githubRepo={pkg.githubRepo}
                    lastFetchedAt={pkg.lastFetchedAt}
                    onClick={() => console.log(`Clicked ${pkg.npmName}`)}
                  />
                ))
              )}
            </div>
          </section>

          {/* Recent Releases Section */}
          <section>
            <h2 className="text-xl font-semibold text-text-primary mb-4">
              Recent Releases
            </h2>
            <div className="space-y-4">
              {releasesLoading ? (
                <p className="text-text-secondary">Loading releases...</p>
              ) : releases?.length === 0 ? (
                <p className="text-text-secondary">
                  No releases found. Releases will appear here after syncing.
                </p>
              ) : (
                releases?.map((release) => (
                  <ReleaseCard
                    key={release.id}
                    tag={release.tag}
                    title={release.title}
                    body={release.body}
                    publishedAt={release.publishedAt}
                    htmlUrl={getReleaseUrl(release)}
                  />
                ))
              )}
            </div>
          </section>
        </Container>
      </main>
    </div>
  )
}
