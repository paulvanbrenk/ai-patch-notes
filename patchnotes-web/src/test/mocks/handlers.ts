import { http, HttpResponse } from 'msw'
import type { Package, Release } from '../../api/types'

const API_BASE = '/api'

export const mockWatchlist: string[] = []

export const mockPackages: Package[] = [
  {
    id: 'pkg_react_001',
    npmName: 'react',
    githubOwner: 'facebook',
    githubRepo: 'react',
    lastFetchedAt: '2026-01-15T10:00:00Z',
    createdAt: '2026-01-01T00:00:00Z',
  },
  {
    id: 'pkg_lodash_002',
    npmName: 'lodash',
    githubOwner: 'lodash',
    githubRepo: 'lodash',
    lastFetchedAt: '2026-01-14T10:00:00Z',
    createdAt: '2026-01-02T00:00:00Z',
  },
]

export const mockReleases: Release[] = [
  {
    id: 'rel_react19_001',
    version: 'v19.0.0',
    title: 'React 19',
    body: 'Major release with new features',
    publishedAt: '2026-01-10T00:00:00Z',
    fetchedAt: '2026-01-15T10:00:00Z',
    major: 19,
    minor: 0,
    isPrerelease: false,
    package: {
      id: 'pkg_react_001',
      npmName: 'react',
      githubOwner: 'facebook',
      githubRepo: 'react',
    },
  },
  {
    id: 'rel_lodash_002',
    version: 'v4.18.0',
    title: 'Lodash 4.18.0',
    body: 'Bug fixes and improvements',
    publishedAt: '2026-01-08T00:00:00Z',
    fetchedAt: '2026-01-14T10:00:00Z',
    major: 4,
    minor: 18,
    isPrerelease: false,
    package: {
      id: 'pkg_lodash_002',
      npmName: 'lodash',
      githubOwner: 'lodash',
      githubRepo: 'lodash',
    },
  },
]

export const handlers = [
  // GET /packages
  http.get(`${API_BASE}/packages`, () => {
    return HttpResponse.json(mockPackages)
  }),

  // GET /packages/:id
  http.get(`${API_BASE}/packages/:id`, ({ params }) => {
    const pkg = mockPackages.find((p) => p.id === params.id)
    if (!pkg) {
      return new HttpResponse(null, { status: 404 })
    }
    return HttpResponse.json(pkg)
  }),

  // POST /packages
  http.post(`${API_BASE}/packages`, async ({ request }) => {
    const body = (await request.json()) as { npmName: string }
    const newPackage: Package = {
      id: 'pkg_new_001',
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
    const body = (await request.json()) as Partial<Package>
    const pkg = mockPackages.find((p) => p.id === params.id)
    if (!pkg) {
      return new HttpResponse(null, { status: 404 })
    }
    return HttpResponse.json({ ...pkg, ...body })
  }),

  // POST /packages/:id/sync
  http.post(`${API_BASE}/packages/:id/sync`, ({ params }) => {
    return HttpResponse.json({
      id: params.id,
      npmName: 'react',
      lastFetchedAt: new Date().toISOString(),
      releasesAdded: 2,
    })
  }),

  // GET /releases
  http.get(`${API_BASE}/releases`, () => {
    return HttpResponse.json(mockReleases)
  }),

  // GET /packages/:id/releases
  http.get(`${API_BASE}/packages/:id/releases`, ({ params }) => {
    const releases = mockReleases.filter((r) => r.package.id === params.id)
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
]
