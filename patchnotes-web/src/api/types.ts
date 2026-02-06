export interface Package {
  id: number
  npmName: string
  githubOwner: string
  githubRepo: string
  lastFetchedAt: string | null
  createdAt: string
  releaseCount?: number
}

export interface Release {
  id: number
  tag: string
  title: string | null
  body: string | null
  publishedAt: string
  fetchedAt: string
  package: {
    id: number
    npmName: string
    githubOwner: string
    githubRepo: string
  }
}

export interface AddPackageRequest {
  npmName: string
}

export interface AddPackageRequest {
  npmName: string
}

export interface AddPackageResponse {
  id: number
  npmName: string
  githubOwner: string
  githubRepo: string
  createdAt: string
}

export interface UpdatePackageRequest {
  githubOwner?: string
  githubRepo?: string
}

export interface SyncPackageResponse {
  id: number
  npmName: string
  lastFetchedAt: string
  releasesAdded: number
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
