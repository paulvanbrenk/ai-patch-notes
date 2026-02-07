import { VersionBadge } from './VersionBadge'

interface TimelineRelease {
  id: string
  tag: string
  title?: string | null
  body?: string | null
  publishedAt: string
  htmlUrl?: string | null
  packageName?: string
}

interface ReleaseTimelineProps {
  releases: TimelineRelease[]
  showPackageName?: boolean
}

function formatDate(dateString: string): string {
  const date = new Date(dateString)
  return date.toLocaleDateString('en-US', {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
  })
}

function formatMonthYear(dateString: string): string {
  const date = new Date(dateString)
  return date.toLocaleDateString('en-US', {
    year: 'numeric',
    month: 'long',
  })
}

function groupByMonth(
  releases: TimelineRelease[]
): Map<string, TimelineRelease[]> {
  const groups = new Map<string, TimelineRelease[]>()

  for (const release of releases) {
    const monthKey = formatMonthYear(release.publishedAt)
    const existing = groups.get(monthKey) || []
    groups.set(monthKey, [...existing, release])
  }

  return groups
}

interface TimelineItemProps {
  release: TimelineRelease
  isLast: boolean
  showPackageName?: boolean
}

function TimelineItem({ release, isLast, showPackageName }: TimelineItemProps) {
  const displayTitle = release.title || release.tag

  return (
    <div className="relative flex gap-4">
      {/* Timeline connector */}
      <div className="flex flex-col items-center">
        {/* Dot */}
        <div className="w-3 h-3 rounded-full bg-brand-500 ring-4 ring-surface-primary z-10" />
        {/* Line */}
        {!isLast && <div className="w-0.5 flex-1 bg-border-default min-h-8" />}
      </div>

      {/* Content */}
      <div className="flex-1 pb-8">
        <div className="bg-surface-primary border border-border-default rounded-lg p-4 shadow-sm hover:shadow-md transition-shadow">
          {/* Header */}
          <div className="flex items-start justify-between gap-4 mb-2">
            <div className="flex items-center gap-3 flex-wrap">
              <VersionBadge version={release.tag} />
              {showPackageName && release.packageName && (
                <span className="text-sm font-medium text-text-secondary">
                  {release.packageName}
                </span>
              )}
            </div>
            <time
              dateTime={release.publishedAt}
              className="text-xs text-text-tertiary whitespace-nowrap"
            >
              {formatDate(release.publishedAt)}
            </time>
          </div>

          {/* Title */}
          <h3 className="font-semibold text-text-primary mb-2">
            {release.htmlUrl ? (
              <a
                href={release.htmlUrl}
                target="_blank"
                rel="noopener noreferrer"
                className="hover:text-brand-600 transition-colors"
              >
                {displayTitle}
              </a>
            ) : (
              displayTitle
            )}
          </h3>

          {/* Body */}
          {release.body && (
            <p className="text-sm text-text-secondary line-clamp-3">
              {release.body}
            </p>
          )}
        </div>
      </div>
    </div>
  )
}

export function ReleaseTimeline({
  releases,
  showPackageName = false,
}: ReleaseTimelineProps) {
  const groupedReleases = groupByMonth(releases)

  return (
    <div className="space-y-8">
      {Array.from(groupedReleases.entries()).map(([month, monthReleases]) => (
        <div key={month}>
          {/* Month header */}
          <div className="flex items-center gap-4 mb-6">
            <h3 className="text-lg font-semibold text-text-primary whitespace-nowrap">
              {month}
            </h3>
            <div className="flex-1 h-px bg-border-default" />
            <span className="text-sm text-text-tertiary">
              {monthReleases.length} release
              {monthReleases.length !== 1 ? 's' : ''}
            </span>
          </div>

          {/* Timeline items */}
          <div className="pl-2">
            {monthReleases.map((release, index) => (
              <TimelineItem
                key={release.id}
                release={release}
                isLast={index === monthReleases.length - 1}
                showPackageName={showPackageName}
              />
            ))}
          </div>
        </div>
      ))}
    </div>
  )
}
