import { useMemo } from 'react'
import { Header, HeaderTitle, Container, Button, Input } from '../components/ui'
import { ReleaseTimeline } from '../components/releases'
import { useReleases } from '../api/hooks'
import type { Release } from '../api/types'

function getReleaseUrl(release: Release): string {
  const { githubOwner, githubRepo } = release.package
  return `https://github.com/${githubOwner}/${githubRepo}/releases/tag/${release.tag}`
}

export function Timeline() {
  const { data: releases, isLoading: releasesLoading } = useReleases()

  const timelineReleases = useMemo(() => {
    if (!releases) return []
    return releases.map((release) => ({
      id: release.id,
      tag: release.tag,
      title: release.title,
      body: release.body,
      publishedAt: release.publishedAt,
      htmlUrl: getReleaseUrl(release),
      packageName: release.package.npmName,
    }))
  }, [releases])

  const uniquePackageCount = useMemo(() => {
    if (!releases) return 0
    const packageIds = new Set(releases.map((r) => r.package.id))
    return packageIds.size
  }, [releases])
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
            <Input placeholder="Filter releases..." className="max-w-md" />
          </div>

          {/* Stats */}
          <div className="flex items-center gap-6 mb-8 text-sm text-text-secondary">
            <span>
              <strong className="text-text-primary">
                {timelineReleases.length}
              </strong>{' '}
              releases
            </span>
            <span>
              <strong className="text-text-primary">
                {uniquePackageCount}
              </strong>{' '}
              packages
            </span>
            <span>Last 7 days</span>
          </div>

          {/* Timeline */}
          {releasesLoading ? (
            <p className="text-text-secondary">Loading releases...</p>
          ) : timelineReleases.length === 0 ? (
            <p className="text-text-secondary">
              No releases found. Releases will appear here after syncing.
            </p>
          ) : (
            <ReleaseTimeline
              releases={timelineReleases}
              showPackageName={true}
            />
          )}
        </Container>
      </main>
    </div>
  )
}
