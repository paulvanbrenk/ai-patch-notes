export interface Package {
  id: number
  npmName: string
  githubOwner: string
  githubRepo: string
  releaseCount: number
  lastFetchedAt: string
}

export interface Release {
  id: number
  tag: string
  title: string | null
  body: string
  publishedAt: string
  htmlUrl: string
}

export interface Notification {
  id: number
  gitHubId: string
  reason: string
  subjectTitle: string
  subjectType: string
  subjectUrl: string | null
  repositoryFullName: string
  unread: boolean
  updatedAt: string
  lastReadAt: string | null
  fetchedAt: string
  package: {
    id: number
    npmName: string
    githubOwner: string
    githubRepo: string
  } | null
}

export interface UnreadCount {
  count: number
}
