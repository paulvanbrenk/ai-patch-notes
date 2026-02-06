import { Card, CardHeader, CardTitle, CardContent } from '../ui'
import { VersionBadge } from './VersionBadge'

interface ReleaseCardProps {
  tag: string
  title?: string | null
  body?: string | null
  publishedAt: string
  htmlUrl?: string | null
  hoverable?: boolean
  onClick?: () => void
}

function formatDate(dateString: string): string {
  const date = new Date(dateString)
  return date.toLocaleDateString('en-US', {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
  })
}

function formatRelativeTime(dateString: string): string {
  const date = new Date(dateString)
  const now = new Date()
  const diffMs = now.getTime() - date.getTime()
  const diffDays = Math.floor(diffMs / (1000 * 60 * 60 * 24))

  if (diffDays === 0) {
    return 'Today'
  }
  if (diffDays === 1) {
    return 'Yesterday'
  }
  if (diffDays < 7) {
    return `${diffDays} days ago`
  }
  if (diffDays < 30) {
    const weeks = Math.floor(diffDays / 7)
    return `${weeks} week${weeks > 1 ? 's' : ''} ago`
  }
  if (diffDays < 365) {
    const months = Math.floor(diffDays / 30)
    return `${months} month${months > 1 ? 's' : ''} ago`
  }
  const years = Math.floor(diffDays / 365)
  return `${years} year${years > 1 ? 's' : ''} ago`
}

export function ReleaseCard({
  tag,
  title,
  body,
  publishedAt,
  htmlUrl,
  hoverable,
  onClick,
}: ReleaseCardProps) {
  const displayTitle = title || tag

  return (
    <Card
      className={
        onClick || hoverable
          ? 'cursor-pointer hover:border-brand-300 hover:shadow-md transition-all'
          : ''
      }
      onClick={onClick}
    >
      <CardHeader>
        <div className="flex items-center gap-3">
          <VersionBadge version={tag} />
          <CardTitle>
            {htmlUrl ? (
              <a
                href={htmlUrl}
                target="_blank"
                rel="noopener noreferrer"
                className="hover:text-brand-600 transition-colors"
                onClick={(e) => e.stopPropagation()}
              >
                {displayTitle}
              </a>
            ) : (
              displayTitle
            )}
          </CardTitle>
        </div>
        <time
          dateTime={publishedAt}
          title={formatDate(publishedAt)}
          className="text-sm text-text-tertiary whitespace-nowrap"
        >
          {formatRelativeTime(publishedAt)}
        </time>
      </CardHeader>

      {body && (
        <CardContent>
          <div className="prose-release text-sm">{body}</div>
        </CardContent>
      )}
    </Card>
  )
}
