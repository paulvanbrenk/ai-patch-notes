export interface Package {
  id: string
  npmName: string
  githubOwner: string
  githubRepo: string
  lastFetchedAt: string | null
  createdAt: string
  releaseCount?: number
}

export interface Release {
  id: string
  version: string
  title: string | null
  body: string | null
  publishedAt: string
  fetchedAt: string
  major: number
  minor: number
  isPrerelease: boolean
  package: {
    id: string
    npmName: string
    githubOwner: string
    githubRepo: string
  }
}

export type SummaryPeriod = 'Week' | 'Month'

export interface Summary {
  id: string
  packageId: string
  versionGroup: string
  period: SummaryPeriod
  periodStart: string
  content: string
  generatedAt: string
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

export interface SyncPackageResponse {
  id: string
  npmName: string
  lastFetchedAt: string
  releasesAdded: number
}
