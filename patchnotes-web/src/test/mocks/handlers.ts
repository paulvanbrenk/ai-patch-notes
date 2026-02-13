import { http, HttpResponse } from 'msw'
import type { PackageDto, PackageDetailDto, ReleaseDto } from '../../api/generated/model'

const API_BASE = '/api'

export const mockWatchlist: string[] = []

export const mockPackages: PackageDto[] = [
  {
    id: 'pkg-react-test-id',
    name: 'react',
    npmName: 'react',
    githubOwner: 'facebook',
    githubRepo: 'react',
    lastFetchedAt: '2026-01-15T10:00:00Z',
    createdAt: '2026-01-01T00:00:00Z',
  },
  {
    id: 'pkg-lodash-test-id',
    name: 'lodash',
    npmName: 'lodash',
    githubOwner: 'lodash',
    githubRepo: 'lodash',
    lastFetchedAt: '2026-01-14T10:00:00Z',
    createdAt: '2026-01-02T00:00:00Z',
  },
]

export const mockPackageDetails: PackageDetailDto[] = mockPackages.map(
  (pkg) => ({ ...pkg, releaseCount: 0 })
)

export const mockReleases: ReleaseDto[] = [
  {
    id: 'rel-react-19-test-id',
    tag: 'v19.0.0',
    title: 'React 19',
    body: 'Major release with new features',
    summary: null,
    summaryGeneratedAt: null,
    publishedAt: '2026-01-10T00:00:00Z',
    fetchedAt: '2026-01-15T10:00:00Z',
    package: {
      id: 'pkg-react-test-id',
      npmName: 'react',
      githubOwner: 'facebook',
      githubRepo: 'react',
    },
  },
  {
    id: 'rel-lodash-418-test-id',
    tag: 'v4.18.0',
    title: 'Lodash 4.18.0',
    body: 'Bug fixes and improvements',
    summary: null,
    summaryGeneratedAt: null,
    publishedAt: '2026-01-08T00:00:00Z',
    fetchedAt: '2026-01-14T10:00:00Z',
    package: {
      id: 'pkg-lodash-test-id',
      npmName: 'lodash',
      githubOwner: 'lodash',
      githubRepo: 'lodash',
    },
  },
]

// Package releases include extra fields in their package object (PackageReleasePackageDto)
export const mockPackageReleases = mockReleases.map((r) => ({
  ...r,
  package: {
    ...r.package,
    name: r.package.npmName ?? `${r.package.githubOwner}/${r.package.githubRepo}`,
  },
}))

export const handlers = [
  // GET /packages
  http.get(`${API_BASE}/packages`, () => {
    return HttpResponse.json(mockPackages)
  }),

  // GET /packages/:id
  http.get(`${API_BASE}/packages/:id`, ({ params }) => {
    const id = params.id as string
    const pkg = mockPackageDetails.find((p) => p.id === id)
    if (!pkg) {
      return new HttpResponse(null, { status: 404 })
    }
    return HttpResponse.json(pkg)
  }),

  // POST /packages
  http.post(`${API_BASE}/packages`, async ({ request }) => {
    const body = (await request.json()) as { npmName: string }
    const newPackage: PackageDto = {
      id: 'pkg-new-test-id',
      name: body.npmName,
      npmName: body.npmName,
      githubOwner: 'owner',
      githubRepo: body.npmName,
      lastFetchedAt: null,
      createdAt: new Date().toISOString(),
    }
    return HttpResponse.json(newPackage, { status: 201 })
  }),

  // DELETE /packages/:id
  http.delete(`${API_BASE}/packages/:id`, () => {
    return new HttpResponse(null, { status: 204 })
  }),

  // PATCH /packages/:id
  http.patch(`${API_BASE}/packages/:id`, async ({ params, request }) => {
    const id = params.id as string
    const body = (await request.json()) as Partial<PackageDto>
    const pkg = mockPackages.find((p) => p.id === id)
    if (!pkg) {
      return new HttpResponse(null, { status: 404 })
    }
    return HttpResponse.json({ ...pkg, ...body })
  }),

  // GET /releases
  http.get(`${API_BASE}/releases`, () => {
    return HttpResponse.json(mockReleases)
  }),

  // GET /releases/:id
  http.get(`${API_BASE}/releases/:id`, ({ params }) => {
    const id = params.id as string
    const release = mockReleases.find((r) => r.id === id)
    if (!release) {
      return new HttpResponse(null, { status: 404 })
    }
    return HttpResponse.json(release)
  }),

  // GET /packages/:id/releases
  http.get(`${API_BASE}/packages/:id/releases`, ({ params }) => {
    const packageId = params.id as string
    const releases = mockPackageReleases.filter(
      (r) => r.package.id === packageId
    )
    return HttpResponse.json(releases)
  }),

  // GET /watchlist
  http.get(`${API_BASE}/watchlist`, () => {
    return HttpResponse.json(mockWatchlist)
  }),

  // PUT /watchlist
  http.put(`${API_BASE}/watchlist`, async ({ request }) => {
    const body = (await request.json()) as { packageIds: string[] }
    mockWatchlist.splice(0, mockWatchlist.length, ...body.packageIds)
    return HttpResponse.json(mockWatchlist)
  }),

  // POST /watchlist/:packageId
  http.post(`${API_BASE}/watchlist/:packageId`, ({ params }) => {
    const packageId = params.packageId as string
    if (mockWatchlist.includes(packageId)) {
      return HttpResponse.json(
        { error: 'Already watching this package' },
        { status: 409 }
      )
    }
    mockWatchlist.push(packageId)
    return HttpResponse.json(packageId, { status: 201 })
  }),

  // DELETE /watchlist/:packageId
  http.delete(`${API_BASE}/watchlist/:packageId`, ({ params }) => {
    const packageId = params.packageId as string
    const index = mockWatchlist.indexOf(packageId)
    if (index !== -1) {
      mockWatchlist.splice(index, 1)
    }
    return new HttpResponse(null, { status: 204 })
  }),

  // GET /subscription/status
  http.get(`${API_BASE}/subscription/status`, () => {
    return HttpResponse.json({ isPro: false, status: null, expiresAt: null })
  }),
]
