import { Card, CardHeader, CardTitle, CardContent } from '../ui'
import { VersionBadge } from './VersionBadge'
import { formatRelativeTime } from '../../utils/dateFormat'

interface ReleaseCardProps {
  tag: string
  title?: string | null
  body?: string | null
  publishedAt?: string
  htmlUrl?: string | null
  hoverable?: boolean
  onClick?: () => void
}

const MARKDOWN_LINK_RE = /^\s*\[([^\]]+)\]\((\S+)\)\s*$/
const BARE_URL_RE = /^\s*(https?:\/\/\S+)\s*$/

function ReleaseBody({ body }: { body: string }) {
  // Markdown link like [Release](https://...)
  const mdMatch = body.match(MARKDOWN_LINK_RE)
  if (mdMatch) {
    return (
      <a
        href={mdMatch[2]}
        target="_blank"
        rel="noopener noreferrer"
        className="text-brand-500 hover:underline"
        onClick={(e) => e.stopPropagation()}
      >
        {mdMatch[1]}
      </a>
    )
  }

  // Bare URL
  const urlMatch = body.match(BARE_URL_RE)
  if (urlMatch) {
    return (
      <a
        href={urlMatch[1]}
        target="_blank"
        rel="noopener noreferrer"
        className="text-brand-500 hover:underline"
        onClick={(e) => e.stopPropagation()}
      >
        View release notes
      </a>
    )
  }

  return <>{body}</>
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
        {publishedAt && (
          <time
            dateTime={publishedAt}
            title={formatDate(publishedAt)}
            className="text-sm text-text-tertiary whitespace-nowrap"
          >
            {formatRelativeTime(publishedAt)}
          </time>
        )}
      </CardHeader>

      {body && (
        <CardContent>
          <div className="prose-release text-sm">
            <ReleaseBody body={body} />
          </div>
        </CardContent>
      )}
    </Card>
  )
}
