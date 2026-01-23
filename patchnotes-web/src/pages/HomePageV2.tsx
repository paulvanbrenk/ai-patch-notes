import { useState, useEffect } from 'react'
import { Link } from '@tanstack/react-router'
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
} from '../components/ui'
import { ThemeToggle } from '../components/theme'
import { UserMenuV2 } from '../components/auth'
import { useFilterStore } from '../stores/filterStore'
import { useSubscriptionStore } from '../stores/subscriptionStore'

// ============================================================================
// Types
// ============================================================================

interface Release {
  id: string
  version: string
  title: string
  publishedAt: string
  body: string
}

interface VersionGroup {
  id: string
  packageName: string
  packageId: string
  versionRange: string
  majorVersion: number
  isPrerelease: boolean
  prereleaseType?: 'canary' | 'beta' | 'alpha' | 'rc' | 'next'
  summary: string
  releaseCount: number
  lastUpdated: string
  releases: Release[]
}

// ============================================================================
// Mock Data
// ============================================================================

const MOCK_VERSION_GROUPS: VersionGroup[] = [
  {
    id: 'next-16-stable',
    packageName: 'Next.js',
    packageId: 'pkg_nextjs',
    versionRange: 'v16.x',
    majorVersion: 16,
    isPrerelease: false,
    summary:
      'Next.js 16 introduces the new App Router as the default, improved server components with streaming SSR, and significant performance improvements. Turbopack is now stable for development builds, offering up to 10x faster refresh times. The new `next/image` component includes automatic format optimization and improved lazy loading.',
    releaseCount: 8,
    lastUpdated: '2026-01-20T14:30:00Z',
    releases: [
      {
        id: 'rel_101',
        version: 'v16.2.0',
        title: 'Next.js 16.2.0',
        publishedAt: '2026-01-20T14:30:00Z',
        body: 'Performance improvements for server components',
      },
      {
        id: 'rel_102',
        version: 'v16.1.0',
        title: 'Next.js 16.1.0',
        publishedAt: '2026-01-15T10:00:00Z',
        body: 'Bug fixes and stability improvements',
      },
      {
        id: 'rel_103',
        version: 'v16.0.0',
        title: 'Next.js 16.0.0',
        publishedAt: '2026-01-10T09:00:00Z',
        body: 'Major release with App Router as default',
      },
    ],
  },
  {
    id: 'next-16-canary',
    packageName: 'Next.js',
    packageId: 'pkg_nextjs',
    versionRange: 'v16.x',
    majorVersion: 16,
    isPrerelease: true,
    prereleaseType: 'canary',
    summary:
      'Experimental features including React 19 Server Actions improvements, partial prerendering enhancements, and new caching strategies. Testing ground for upcoming stable features.',
    releaseCount: 24,
    lastUpdated: '2026-01-21T08:15:00Z',
    releases: [
      {
        id: 'rel_104',
        version: 'v16.3.0-canary.12',
        title: 'Next.js 16.3.0-canary.12',
        publishedAt: '2026-01-21T08:15:00Z',
        body: 'Experimental: New partial prerendering API',
      },
      {
        id: 'rel_105',
        version: 'v16.3.0-canary.11',
        title: 'Next.js 16.3.0-canary.11',
        publishedAt: '2026-01-20T16:00:00Z',
        body: 'Fix: Server action serialization',
      },
    ],
  },
  {
    id: 'react-19-stable',
    packageName: 'React',
    packageId: 'pkg_react',
    versionRange: 'v19.x',
    majorVersion: 19,
    isPrerelease: false,
    summary:
      'React 19 brings Actions for handling async operations in transitions, new hooks including `useOptimistic` and `useFormStatus`, and the new `use` API for reading resources in render. Server Components are now fully supported with improved streaming.',
    releaseCount: 4,
    lastUpdated: '2026-01-18T11:00:00Z',
    releases: [
      {
        id: 'rel_201',
        version: 'v19.1.0',
        title: 'React 19.1.0',
        publishedAt: '2026-01-18T11:00:00Z',
        body: 'Improved Actions error handling',
      },
      {
        id: 'rel_202',
        version: 'v19.0.0',
        title: 'React 19.0.0',
        publishedAt: '2026-01-05T09:00:00Z',
        body: 'Major release introducing Actions and new hooks',
      },
    ],
  },
  {
    id: 'typescript-5-stable',
    packageName: 'TypeScript',
    packageId: 'pkg_typescript',
    versionRange: 'v5.x',
    majorVersion: 5,
    isPrerelease: false,
    summary:
      'TypeScript 5.7 includes improved type inference for computed properties, faster incremental builds with isolated declarations, and new decorators syntax support. The `--verbatimModuleSyntax` flag is now default for new projects.',
    releaseCount: 12,
    lastUpdated: '2026-01-19T15:45:00Z',
    releases: [
      {
        id: 'rel_301',
        version: 'v5.7.3',
        title: 'TypeScript 5.7.3',
        publishedAt: '2026-01-19T15:45:00Z',
        body: 'Bug fix release',
      },
      {
        id: 'rel_302',
        version: 'v5.7.2',
        title: 'TypeScript 5.7.2',
        publishedAt: '2026-01-12T10:00:00Z',
        body: 'Performance improvements',
      },
    ],
  },
  {
    id: 'typescript-5-beta',
    packageName: 'TypeScript',
    packageId: 'pkg_typescript',
    versionRange: 'v5.8',
    majorVersion: 5,
    isPrerelease: true,
    prereleaseType: 'beta',
    summary:
      'Preview of TypeScript 5.8 featuring improved inference for generic functions, new `using` keyword support for resource management, and experimental isolated modules mode.',
    releaseCount: 3,
    lastUpdated: '2026-01-17T09:30:00Z',
    releases: [
      {
        id: 'rel_303',
        version: 'v5.8.0-beta',
        title: 'TypeScript 5.8.0 Beta',
        publishedAt: '2026-01-17T09:30:00Z',
        body: 'Beta release for testing',
      },
    ],
  },
  {
    id: 'vite-6-stable',
    packageName: 'Vite',
    packageId: 'pkg_vite',
    versionRange: 'v6.x',
    majorVersion: 6,
    isPrerelease: false,
    summary:
      'Vite 6 delivers Environment API for unified dev/build environments, improved Rolldown integration for faster production builds, and enhanced HMR with module graph visualization. CSS processing is now 2x faster with new Lightning CSS backend.',
    releaseCount: 6,
    lastUpdated: '2026-01-16T13:20:00Z',
    releases: [
      {
        id: 'rel_401',
        version: 'v6.1.0',
        title: 'Vite 6.1.0',
        publishedAt: '2026-01-16T13:20:00Z',
        body: 'Environment API improvements',
      },
      {
        id: 'rel_402',
        version: 'v6.0.0',
        title: 'Vite 6.0.0',
        publishedAt: '2026-01-02T09:00:00Z',
        body: 'Major release with Environment API',
      },
    ],
  },
]

// ============================================================================
// Utility Functions
// ============================================================================

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
                      {release.title}
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

export function HomePageV2() {
  const {
    showPrerelease,
    sortBy,
    groupByPackage,
    togglePrerelease,
    setSortBy,
    toggleGroupByPackage,
  } = useFilterStore()
  const [expandedGroups, setExpandedGroups] = useState<Set<string>>(new Set())
  const [isLoading] = useState(false)

  const { user } = useStytchUser()
  const { isPro, checkSubscription, startCheckout } = useSubscriptionStore()

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

  // Filter groups based on prerelease toggle
  const filteredGroups = showPrerelease
    ? MOCK_VERSION_GROUPS
    : MOCK_VERSION_GROUPS.filter((g) => !g.isPrerelease)

  // Sort groups
  const sortedGroups = [...filteredGroups].sort((a, b) => {
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
        <HeaderTitle>Patch Notes</HeaderTitle>
        <div className="flex items-center gap-2">
          <Link to="/">
            <Button variant="ghost" size="sm">
              Back
            </Button>
          </Link>
          <Badge variant="prerelease">Preview</Badge>
          <div className="w-px h-6 bg-border-muted mx-1" />
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
          <UserMenuV2 />
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
          {!isLoading && filteredGroups.length === 0 && (
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
