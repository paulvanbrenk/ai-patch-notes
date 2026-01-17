export interface Package {
  id: number;
  npmName: string;
  githubOwner: string;
  githubRepo: string;
  releaseCount: number;
  lastFetchedAt: string;
}

export interface Release {
  id: number;
  tag: string;
  title: string | null;
  body: string;
  publishedAt: string;
  htmlUrl: string;
}
