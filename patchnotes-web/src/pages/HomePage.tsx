import { useState, useEffect, useMemo } from 'react'
import { useStytchUser } from '@stytch/react'
import {
  FlaskConical,
  FlaskConicalOff,
  ArrowDownAZ,
  CalendarArrowDown,
  Group,
  Sparkles,
} from 'lucide-react'
import {
  Header,
  HeaderTitle,
  Container,
  Button,
  Badge,
  Card,
  Logo,
} from '../components/ui'
import { ThemeToggle } from '../components/theme'
import { UserMenu } from '../components/auth'
import { useFilterStore } from '../stores/filterStore'
import { useSubscriptionStore } from '../stores/subscriptionStore'
import { usePackages, useReleases } from '../api/hooks'
import type { Release as ApiRelease } from '../api/types'

// ============================================================================
// Types
// ============================================================================

type PrereleaseType = 'canary' | 'beta' | 'alpha' | 'rc' | 'next'

interface VersionGroup {
  id: string
  packageName: string
  packageId: string
  versionRange: string
  majorVersion: number
  isPrerelease: boolean
  prereleaseType?: PrereleaseType
  summary: string
  releaseCount: number
  lastUpdated: string
  releases: ApiRelease[]
}

// Utility Functions
// ============================================================================

function parseMajorVersion(tag: string): number {
  const match = tag.match(/^v?(\d+)/)
  return match ? parseInt(match[1], 10) : 0
}

function detectPrereleaseType(tag: string): PrereleaseType | undefined {
  const lower = tag.toLowerCase()
  if (lower.includes('canary')) return 'canary'
  if (lower.includes('alpha')) return 'alpha'
  if (lower.includes('beta')) return 'beta'
  if (lower.includes('next')) return 'next'
  if (lower.includes('rc')) return 'rc'
  return undefined
}

function isPrerelease(tag: string): boolean {
  return /-(alpha|beta|rc|next|canary|dev|preview)/i.test(tag)
}

function buildVersionGroups(
  releases: ApiRelease[],
  packageNames: Map<number, string>
): VersionGroup[] {
  const groupMap = new Map<string, VersionGroup>()

  for (const release of releases) {
    const packageId = release.package.id
    const major = parseMajorVersion(release.tag)
    const prerelease = isPrerelease(release.tag)
    const key = `${packageId}-${major}-${prerelease}`

    let group = groupMap.get(key)
    if (!group) {
      const packageName = packageNames.get(packageId) ?? release.package.npmName
      group = {
        id: key,
        packageName,
        packageId,
        versionRange: `v${major}.x`,
        majorVersion: major,
        isPrerelease: prerelease,
        prereleaseType: prerelease
          ? detectPrereleaseType(release.tag)
          : undefined,
        summary: '',
        releaseCount: 0,
        lastUpdated: release.publishedAt,
        releases: [],
      }
      groupMap.set(key, group)
    }

    group.releases.push(release)
    group.releaseCount++

    if (
      new Date(release.publishedAt).getTime() >
      new Date(group.lastUpdated).getTime()
    ) {
      group.lastUpdated = release.publishedAt
    }
  }

  // Sort releases within each group by publishedAt descending and build summary
  for (const group of groupMap.values()) {
    group.releases.sort(
      (a, b) =>
        new Date(b.publishedAt).getTime() - new Date(a.publishedAt).getTime()
    )

    // Build a placeholder summary from release titles
    const titles = group.releases
      .slice(0, 3)
      .map((r) => r.title || r.tag)
      .join(', ')
    const extra =
      group.releaseCount > 3 ? ` and ${group.releaseCount - 3} more` : ''
    group.summary = `${group.releaseCount} release${group.releaseCount !== 1 ? 's' : ''} in this version: ${titles}${extra}.`
  }

  return Array.from(groupMap.values())
}

function formatRelativeTime(dateString: string): string {
  const date = new Date(dateString)
  const now = new Date()
  const diffMs = now.getTime() - date.getTime()
  const diffDays = Math.floor(diffMs / (1000 * 60 * 60 * 24))
  const diffHours = Math.floor(diffMs / (1000 * 60 * 60))

  if (diffHours < 1) return 'Just now'
  if (diffHours < 24) return `${diffHours}h ago`
  if (diffDays === 1) return 'Yesterday'
  if (diffDays < 7) return `${diffDays}d ago`
  if (diffDays < 30) return `${Math.floor(diffDays / 7)}w ago`
  return `${Math.floor(diffDays / 30)}mo ago`
}

function formatDate(dateString: string): string {
  return new Date(dateString).toLocaleDateString('en-US', {
    month: 'short',
    day: 'numeric',
    year: 'numeric',
  })
}

// ============================================================================
// Components
// ============================================================================

function SkeletonCard() {
  return (
    <Card className="animate-pulse">
      <div className="flex items-start justify-between gap-4 mb-4">
        <div className="flex items-center gap-3">
          <div className="w-10 h-10 rounded-lg bg-surface-tertiary" />
          <div>
            <div className="h-5 w-32 bg-surface-tertiary rounded mb-2" />
            <div className="h-4 w-20 bg-surface-tertiary rounded" />
          </div>
        </div>
        <div className="h-6 w-16 bg-surface-tertiary rounded-full" />
      </div>
      <div className="space-y-2">
        <div className="h-4 w-full bg-surface-tertiary rounded" />
        <div className="h-4 w-5/6 bg-surface-tertiary rounded" />
        <div className="h-4 w-4/6 bg-surface-tertiary rounded" />
      </div>
      <div className="flex items-center gap-4 mt-4 pt-4 border-t border-border-muted">
        <div className="h-4 w-24 bg-surface-tertiary rounded" />
        <div className="h-4 w-20 bg-surface-tertiary rounded" />
      </div>
    </Card>
  )
}

function PackageIcon({ name }: { name: string }) {
  // Brand colors for well-known packages
  const icons: Record<string, { bg: string; text: string; icon?: string }> = {
    'Next.js': {
      bg: 'bg-black',
      text: 'text-white',
    },
    React: {
      bg: 'bg-sky-500/15',
      text: 'text-sky-600 dark:text-sky-400',
    },
    TypeScript: {
      bg: 'bg-blue-500/15',
      text: 'text-blue-600 dark:text-blue-400',
    },
    Vite: {
      bg: 'bg-violet-500/15',
      text: 'text-violet-600 dark:text-violet-400',
    },
  }
  const { bg, text } = icons[name] || {
    bg: 'bg-brand-100 dark:bg-brand-900/30',
    text: 'text-brand-600 dark:text-brand-400',
  }

  const initial = name.charAt(0).toUpperCase()

  return (
    <div
      className={`w-10 h-10 rounded-lg flex items-center justify-center font-semibold text-lg ${bg} ${text}`}
    >
      {initial}
    </div>
  )
}

function PrereleaseTag({ type }: { type?: string }) {
  if (!type) return null

  const colors: Record<string, string> = {
    canary:
      'bg-orange-100 text-orange-900 dark:bg-orange-900/40 dark:text-orange-200',
    beta: 'bg-blue-50 text-blue-800 ring-1 ring-inset ring-blue-600/20 dark:bg-blue-900/30 dark:text-blue-300 dark:ring-blue-500/30',
    alpha:
      'bg-purple-50 text-purple-800 ring-1 ring-inset ring-purple-600/20 dark:bg-purple-900/30 dark:text-purple-300 dark:ring-purple-500/30',
    rc: 'bg-emerald-50 text-emerald-800 ring-1 ring-inset ring-emerald-600/20 dark:bg-emerald-900/30 dark:text-emerald-300 dark:ring-emerald-500/30',
    next: 'bg-pink-50 text-pink-800 ring-1 ring-inset ring-pink-600/20 dark:bg-pink-900/30 dark:text-pink-300 dark:ring-pink-500/30',
  }

  return (
    <span
      className={`inline-flex items-center px-2 py-0.5 text-xs font-medium rounded-full ${colors[type] || colors.beta}`}
    >
      {type}
    </span>
  )
}

function SummaryCard({
  group,
  isExpanded,
  onToggle,
}: {
  group: VersionGroup
  isExpanded: boolean
  onToggle: () => void
}) {
  return (
    <Card
      padding="none"
      className="overflow-hidden hover:shadow-md transition-shadow"
    >
      {/* Main Summary Section */}
      <div className="p-5">
        {/* Header */}
        <div className="flex items-start justify-between gap-4 mb-3">
          <div className="flex items-center gap-3">
            <PackageIcon name={group.packageName} />
            <div>
              <div className="flex items-center gap-2">
                <h3 className="font-semibold text-text-primary">
                  {group.packageName}
                </h3>
                <span className="text-sm font-mono text-text-secondary">
                  {group.versionRange}
                </span>
              </div>
              <div className="flex items-center gap-2 mt-0.5">
                {group.isPrerelease ? (
                  <PrereleaseTag type={group.prereleaseType} />
                ) : (
                  <Badge variant="minor">stable</Badge>
                )}
              </div>
            </div>
          </div>
          <time
            dateTime={group.lastUpdated}
            title={formatDate(group.lastUpdated)}
            className="text-sm text-text-tertiary whitespace-nowrap"
          >
            {formatRelativeTime(group.lastUpdated)}
          </time>
        </div>

        {/* Summary */}
        <p className="text-sm text-text-secondary leading-relaxed">
          {group.summary}
        </p>

        {/* Footer */}
        <div className="flex items-center justify-between mt-4 pt-4 border-t border-border-muted">
          <div className="flex items-center gap-4 text-sm text-text-tertiary">
            <span className="flex items-center gap-1.5">
              <svg
                className="w-4 h-4"
                fill="none"
                viewBox="0 0 24 24"
                stroke="currentColor"
              >
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  strokeWidth={1.5}
                  d="M7 7h.01M7 3h5c.512 0 1.024.195 1.414.586l7 7a2 2 0 010 2.828l-7 7a2 2 0 01-2.828 0l-7-7A2 2 0 013 12V7a4 4 0 014-4z"
                />
              </svg>
              {group.releaseCount} release{group.releaseCount !== 1 && 's'}
            </span>
          </div>
          <button
            onClick={onToggle}
            className="flex items-center gap-1.5 text-sm font-medium text-brand-600 hover:text-brand-700 transition-colors"
          >
            {isExpanded ? 'Hide releases' : 'Show releases'}
            <svg
              className={`w-4 h-4 transition-transform ${isExpanded ? 'rotate-180' : ''}`}
              fill="none"
              viewBox="0 0 24 24"
              stroke="currentColor"
            >
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                strokeWidth={2}
                d="M19 9l-7 7-7-7"
              />
            </svg>
          </button>
        </div>
      </div>

      {/* Expanded Releases */}
      {isExpanded && (
        <div className="border-t border-border-default bg-surface-secondary/50">
          <div className="divide-y divide-border-muted">
            {group.releases.map((release) => (
              <div
                key={release.id}
                className="px-5 py-3 hover:bg-surface-tertiary/50 transition-colors"
              >
                <div className="flex items-center justify-between gap-4">
                  <div className="flex items-center gap-3">
                    <code className="text-sm font-mono text-brand-600 bg-brand-50 dark:bg-brand-900/20 px-2 py-0.5 rounded">
                      {release.version}
                    </code>
                    <span className="text-sm text-text-primary">
                      {release.title ?? release.tag}
                    </span>
                  </div>
                  <time className="text-xs text-text-tertiary whitespace-nowrap">
                    {formatDate(release.publishedAt)}
                  </time>
                </div>
                {release.body && (
                  <p className="mt-1.5 text-sm text-text-secondary pl-[calc(theme(spacing.3)+theme(spacing.2)+4ch)]">
                    {release.body}
                  </p>
                )}
              </div>
            ))}
          </div>
          <div className="px-5 py-3 bg-surface-tertiary/30">
            <a
              href="#"
              className="text-sm text-brand-600 hover:text-brand-700 font-medium"
            >
              View all {group.releaseCount} releases →
            </a>
          </div>
        </div>
      )}
    </Card>
  )
}

function FilterButton({
  active,
  onClick,
  children,
  title,
}: {
  active: boolean
  onClick: () => void
  children: React.ReactNode
  title?: string
}) {
  return (
    <button
      onClick={onClick}
      title={title}
      className={`
        flex items-center gap-1.5 px-3 py-1.5 text-sm font-medium rounded-lg transition-all
        ${
          active
            ? 'bg-surface-primary text-text-primary shadow-sm ring-1 ring-border-default'
            : 'text-text-secondary hover:text-text-primary hover:bg-surface-primary/50'
        }
      `}
    >
      {children}
    </button>
  )
}

// ============================================================================
// Main Component
// ============================================================================

export function HomePage() {
  const {
    showPrerelease,
    sortBy,
    groupByPackage,
    togglePrerelease,
    setSortBy,
    toggleGroupByPackage,
  } = useFilterStore()
  const [expandedGroups, setExpandedGroups] = useState<Set<string>>(new Set())

  const { user } = useStytchUser()
  const { isPro, checkSubscription, startCheckout } = useSubscriptionStore()

  // Fetch real data
  const { data: packages, isLoading: packagesLoading } = usePackages()
  const { data: releases, isLoading: releasesLoading } = useReleases(
    showPrerelease ? undefined : { excludePrerelease: true }
  )

  const isLoading = packagesLoading || releasesLoading

  // Build a packageId -> display name map from packages
  const packageNames = useMemo(() => {
    const map = new Map<number, string>()
    if (packages) {
      for (const pkg of packages) {
        map.set(pkg.id, pkg.npmName)
      }
    }
    return map
  }, [packages])

  // Group releases into VersionGroup objects
  const versionGroups = useMemo(
    () => buildVersionGroups(releases ?? [], packageNames),
    [releases, packageNames]
  )

  // Check subscription status when user is logged in
  useEffect(() => {
    if (user) {
      checkSubscription()
    }
  }, [user, checkSubscription])

  const handleUpgrade = () => {
    if (!user) {
      window.location.href = '/login'
      return
    }
    startCheckout()
  }

  const toggleExpanded = (groupId: string) => {
    setExpandedGroups((prev) => {
      const next = new Set(prev)
      if (next.has(groupId)) {
        next.delete(groupId)
      } else {
        next.add(groupId)
      }
      return next
    })
  }

  // Sort groups
  const sortedGroups = [...versionGroups].sort((a, b) => {
    if (sortBy === 'name') {
      return a.packageName.localeCompare(b.packageName)
    }
    // Sort by date (most recent first)
    return new Date(b.lastUpdated).getTime() - new Date(a.lastUpdated).getTime()
  })

  // Group by package for display
  const groupedByPackageMap = sortedGroups.reduce(
    (acc, group) => {
      if (!acc[group.packageName]) {
        acc[group.packageName] = []
      }
      acc[group.packageName].push(group)
      return acc
    },
    {} as Record<string, VersionGroup[]>
  )

  return (
    <div className="min-h-screen bg-surface-secondary">
      <Header>
        <div className="flex items-center gap-3">
          <Logo />
          <div>
            <HeaderTitle>Patch Notes</HeaderTitle>
            <p className="text-sm font-normal text-text-tertiary">
              by Tiny Tools
            </p>
          </div>
        </div>
        <div className="flex items-center gap-2">
          {user && !isPro && (
            <Button
              variant="primary"
              size="sm"
              onClick={handleUpgrade}
              className="flex items-center gap-1.5"
            >
              <Sparkles className="w-4 h-4" />
              Upgrade
            </Button>
          )}
          <ThemeToggle />
          <UserMenu />
        </div>
      </Header>

      <main className="py-8">
        <Container>
          {/* Filters */}
          <div className="flex items-center justify-end gap-2 mb-6">
            <FilterButton
              active={showPrerelease}
              onClick={togglePrerelease}
              title={showPrerelease ? 'Hide pre-releases' : 'Show pre-releases'}
            >
              {showPrerelease ? (
                <FlaskConical className="w-4 h-4" />
              ) : (
                <FlaskConicalOff className="w-4 h-4" />
              )}
            </FilterButton>
            <FilterButton
              active={groupByPackage}
              onClick={toggleGroupByPackage}
              title={groupByPackage ? 'Disable grouping' : 'Group by package'}
            >
              <Group className="w-4 h-4" />
            </FilterButton>
            <div className="flex items-center rounded-lg border border-border-default overflow-hidden">
              <button
                onClick={() => setSortBy('name')}
                title="Sort by name"
                className={`flex items-center gap-1.5 px-3 py-1.5 text-sm font-medium transition-colors
                  ${
                    sortBy === 'name'
                      ? 'bg-surface-primary text-text-primary'
                      : 'bg-transparent text-text-secondary hover:text-text-primary hover:bg-surface-tertiary/50'
                  }`}
              >
                <ArrowDownAZ className="w-4 h-4" />
              </button>
              <div className="w-px h-5 bg-border-default" />
              <button
                onClick={() => setSortBy('date')}
                title="Sort by date"
                className={`flex items-center gap-1.5 px-3 py-1.5 text-sm font-medium transition-colors
                  ${
                    sortBy === 'date'
                      ? 'bg-surface-primary text-text-primary'
                      : 'bg-transparent text-text-secondary hover:text-text-primary hover:bg-surface-tertiary/50'
                  }`}
              >
                <CalendarArrowDown className="w-4 h-4" />
              </button>
            </div>
          </div>

          {/* Content */}
          {isLoading ? (
            <div className="space-y-4">
              <SkeletonCard />
              <SkeletonCard />
              <SkeletonCard />
            </div>
          ) : groupByPackage ? (
            <div className="space-y-8">
              {Object.entries(groupedByPackageMap).map(
                ([packageName, groups]) => (
                  <section key={packageName}>
                    <h2 className="text-lg font-semibold text-text-primary mb-4 flex items-center gap-2">
                      <PackageIcon name={packageName} />
                      {packageName}
                      <span className="text-sm font-normal text-text-tertiary">
                        ({groups.length} version
                        {groups.length !== 1 && 's'})
                      </span>
                    </h2>
                    <div className="space-y-4">
                      {groups.map((group) => (
                        <SummaryCard
                          key={group.id}
                          group={group}
                          isExpanded={expandedGroups.has(group.id)}
                          onToggle={() => toggleExpanded(group.id)}
                        />
                      ))}
                    </div>
                  </section>
                )
              )}
            </div>
          ) : (
            <div className="space-y-4">
              {sortedGroups.map((group) => (
                <SummaryCard
                  key={group.id}
                  group={group}
                  isExpanded={expandedGroups.has(group.id)}
                  onToggle={() => toggleExpanded(group.id)}
                />
              ))}
            </div>
          )}

          {/* Empty State */}
          {!isLoading && versionGroups.length === 0 && (
            <div className="text-center py-16">
              <div className="w-16 h-16 mx-auto mb-4 rounded-full bg-surface-tertiary flex items-center justify-center">
                <FlaskConicalOff className="w-8 h-8 text-text-tertiary" />
              </div>
              <h3 className="text-lg font-semibold text-text-primary mb-2">
                No releases found
              </h3>
              <p className="text-text-secondary">
                Try adjusting your filters to see more releases.
              </p>
            </div>
          )}
        </Container>
      </main>
    </div>
  )
}
