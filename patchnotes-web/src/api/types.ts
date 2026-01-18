export interface Package {
  id: number
  npmName: string
  githubOwner: string
  githubRepo: string
  lastFetchedAt: string | null
  createdAt: string
}

export interface Release {
  id: number
  tag: string
  title: string | null
  body: string
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
