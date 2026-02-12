export interface Package {
  id: string
  npmName: string | null
  githubOwner: string
  githubRepo: string
  lastFetchedAt: string | null
  createdAt: string
  releaseCount?: number
}

export interface Release {
  id: string
  tag: string
  title: string | null
  body: string | null
  summary: string | null
  summaryGeneratedAt: string | null
  publishedAt: string
  fetchedAt: string
  package: {
    id: string
    npmName: string | null
    githubOwner: string
    githubRepo: string
  }
}

export interface AddPackageRequest {
  npmName: string
}

export interface AddPackageResponse {
  id: string
  npmName: string
  githubOwner: string
  githubRepo: string
  createdAt: string
}

export interface UpdatePackageRequest {
  githubOwner?: string
  githubRepo?: string
}
