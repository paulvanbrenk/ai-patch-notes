import { useStytchUser } from '@stytch/react'
import { Eye, EyeOff, Loader2 } from 'lucide-react'
import { Card, CardHeader, CardTitle, CardContent, Badge } from '../ui'
import {
  useWatchlist,
  useAddToWatchlist,
  useRemoveFromWatchlist,
} from '../../api/hooks'

interface PackageCardProps {
  packageId?: string
  npmName: string
  githubOwner: string
  githubRepo: string
  releaseCount?: number
  lastFetchedAt?: string | null
  onClick?: () => void
  hoverable?: boolean
}

function formatDate(dateString: string): string {
  const date = new Date(dateString)
  return date.toLocaleDateString('en-US', {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  })
}

export function PackageCard({
  packageId,
  npmName,
  githubOwner,
  githubRepo,
  releaseCount,
  lastFetchedAt,
  onClick,
  hoverable,
}: PackageCardProps) {
  const { user } = useStytchUser()
  const { data: watchlist } = useWatchlist()
  const addToWatchlist = useAddToWatchlist()
  const removeFromWatchlist = useRemoveFromWatchlist()

  const isWatched = packageId
    ? (watchlist?.includes(packageId) ?? false)
    : false
  const isMutating = addToWatchlist.isPending || removeFromWatchlist.isPending

  const handleWatchToggle = (e: React.MouseEvent) => {
    e.stopPropagation()
    if (!packageId || isMutating) return
    if (isWatched) {
      removeFromWatchlist.mutate(packageId)
    } else {
      addToWatchlist.mutate(packageId)
    }
  }

  const githubUrl = `https://github.com/${githubOwner}/${githubRepo}`
  const npmUrl = `https://www.npmjs.com/package/${npmName}`

  const content = (
    <>
      <CardHeader>
        <div className="flex items-center gap-3">
          <div className="w-10 h-10 rounded-lg bg-surface-tertiary flex items-center justify-center">
            <svg
              className="w-5 h-5 text-text-secondary"
              viewBox="0 0 24 24"
              fill="currentColor"
            >
              <path d="M20 3H4a1 1 0 0 0-1 1v16a1 1 0 0 0 1 1h16a1 1 0 0 0 1-1V4a1 1 0 0 0-1-1zm-1 16H5V5h14v14z" />
              <path d="M6 7h12v2H6zm0 4h12v2H6zm0 4h7v2H6z" />
            </svg>
          </div>
          <div>
            <CardTitle>{npmName}</CardTitle>
            <p className="text-sm text-text-tertiary mt-0.5">
              {githubOwner}/{githubRepo}
            </p>
          </div>
        </div>
        <div className="flex items-center gap-2">
          {user && packageId && (
            <button
              onClick={handleWatchToggle}
              disabled={isMutating}
              title={isWatched ? 'Unwatch package' : 'Watch package'}
              className={`inline-flex items-center gap-1.5 px-2.5 py-1 text-xs font-medium rounded-md transition-colors disabled:opacity-50 ${
                isWatched
                  ? 'bg-brand-50 text-brand-600 hover:bg-brand-100 dark:bg-brand-900/20 dark:text-brand-400 dark:hover:bg-brand-900/30'
                  : 'bg-surface-tertiary text-text-secondary hover:bg-surface-tertiary/80 hover:text-text-primary'
              }`}
            >
              {isMutating ? (
                <Loader2 className="w-3.5 h-3.5 animate-spin" />
              ) : isWatched ? (
                <Eye className="w-3.5 h-3.5" />
              ) : (
                <EyeOff className="w-3.5 h-3.5" />
              )}
              {isWatched ? 'Watching' : 'Watch'}
            </button>
          )}
          {releaseCount !== undefined && (
            <Badge>
              {releaseCount} release{releaseCount !== 1 ? 's' : ''}
            </Badge>
          )}
        </div>
      </CardHeader>

      <CardContent>
        <div className="flex items-center gap-4 text-sm">
          <a
            href={githubUrl}
            target="_blank"
            rel="noopener noreferrer"
            className="inline-flex items-center gap-1.5 text-text-secondary hover:text-brand-600 transition-colors"
            onClick={(e) => e.stopPropagation()}
          >
            <svg className="w-4 h-4" viewBox="0 0 24 24" fill="currentColor">
              <path d="M12 0C5.37 0 0 5.37 0 12c0 5.31 3.435 9.795 8.205 11.385.6.105.825-.255.825-.57 0-.285-.015-1.23-.015-2.235-3.015.555-3.795-.735-4.035-1.41-.135-.345-.72-1.41-1.23-1.695-.42-.225-1.02-.78-.015-.795.945-.015 1.62.87 1.845 1.23 1.08 1.815 2.805 1.305 3.495.99.105-.78.42-1.305.765-1.605-2.67-.3-5.46-1.335-5.46-5.925 0-1.305.465-2.385 1.23-3.225-.12-.3-.54-1.53.12-3.18 0 0 1.005-.315 3.3 1.23.96-.27 1.98-.405 3-.405s2.04.135 3 .405c2.295-1.56 3.3-1.23 3.3-1.23.66 1.65.24 2.88.12 3.18.765.84 1.23 1.905 1.23 3.225 0 4.605-2.805 5.625-5.475 5.925.435.375.81 1.095.81 2.22 0 1.605-.015 2.895-.015 3.3 0 .315.225.69.825.57A12.02 12.02 0 0 0 24 12c0-6.63-5.37-12-12-12z" />
            </svg>
            GitHub
          </a>
          <a
            href={npmUrl}
            target="_blank"
            rel="noopener noreferrer"
            className="inline-flex items-center gap-1.5 text-text-secondary hover:text-brand-600 transition-colors"
            onClick={(e) => e.stopPropagation()}
          >
            <svg className="w-4 h-4" viewBox="0 0 24 24" fill="currentColor">
              <path d="M0 7.334v8h6.666v1.332H12v-1.332h12v-8H0zm6.666 6.664H5.334v-4H3.999v4H1.335V8.667h5.331v5.331zm4 0v1.336H8.001V8.667h5.334v5.332h-2.669v-.001zm12.001 0h-1.33v-4h-1.336v4h-1.335v-4h-1.33v4h-2.671V8.667h8.002v5.331z" />
            </svg>
            npm
          </a>
          {lastFetchedAt && (
            <span className="text-text-tertiary ml-auto">
              Updated {formatDate(lastFetchedAt)}
            </span>
          )}
        </div>
      </CardContent>
    </>
  )

  if (onClick || hoverable) {
    return (
      <Card
        className="cursor-pointer hover:border-brand-300 hover:shadow-md transition-all"
        onClick={onClick}
      >
        {content}
      </Card>
    )
  }

  return <Card>{content}</Card>
}
