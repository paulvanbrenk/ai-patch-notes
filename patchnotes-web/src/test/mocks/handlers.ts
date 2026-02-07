import { http, HttpResponse } from 'msw'
import type {
  Package,
  Release,
  Notification,
  UnreadCount,
} from '../../api/types'

const API_BASE = '/api'

export const mockWatchlist: number[] = []

export const mockPackages: Package[] = [
  {
    id: 1,
    npmName: 'react',
    githubOwner: 'facebook',
    githubRepo: 'react',
    lastFetchedAt: '2026-01-15T10:00:00Z',
    createdAt: '2026-01-01T00:00:00Z',
  },
  {
    id: 2,
    npmName: 'lodash',
    githubOwner: 'lodash',
    githubRepo: 'lodash',
    lastFetchedAt: '2026-01-14T10:00:00Z',
    createdAt: '2026-01-02T00:00:00Z',
  },
]

export const mockReleases: Release[] = [
  {
    id: 1,
    tag: 'v19.0.0',
    title: 'React 19',
    body: 'Major release with new features',
    publishedAt: '2026-01-10T00:00:00Z',
    fetchedAt: '2026-01-15T10:00:00Z',
    package: {
      id: 1,
      npmName: 'react',
      githubOwner: 'facebook',
      githubRepo: 'react',
    },
  },
  {
    id: 2,
    tag: 'v4.18.0',
    title: 'Lodash 4.18.0',
    body: 'Bug fixes and improvements',
    publishedAt: '2026-01-08T00:00:00Z',
    fetchedAt: '2026-01-14T10:00:00Z',
    package: {
      id: 2,
      npmName: 'lodash',
      githubOwner: 'lodash',
      githubRepo: 'lodash',
    },
  },
]

export const mockNotifications: Notification[] = [
  {
    id: 1,
    gitHubId: 'gh-123',
    reason: 'subscribed',
    subjectTitle: 'New release available',
    subjectType: 'Release',
    subjectUrl: 'https://github.com/facebook/react/releases/v19.0.0',
    repositoryFullName: 'facebook/react',
    unread: true,
    updatedAt: '2026-01-15T10:00:00Z',
    lastReadAt: null,
    fetchedAt: '2026-01-15T10:00:00Z',
    package: {
      id: 1,
      npmName: 'react',
      githubOwner: 'facebook',
      githubRepo: 'react',
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
    const id = Number(params.id)
    const pkg = mockPackages.find((p) => p.id === id)
    if (!pkg) {
      return new HttpResponse(null, { status: 404 })
    }
    return HttpResponse.json(pkg)
  }),

  // POST /packages
  http.post(`${API_BASE}/packages`, async ({ request }) => {
    const body = (await request.json()) as { npmName: string }
    const newPackage: Package = {
      id: mockPackages.length + 1,
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
    const id = Number(params.id)
    const body = (await request.json()) as Partial<Package>
    const pkg = mockPackages.find((p) => p.id === id)
    if (!pkg) {
      return new HttpResponse(null, { status: 404 })
    }
    return HttpResponse.json({ ...pkg, ...body })
  }),

  // POST /packages/:id/sync
  http.post(`${API_BASE}/packages/:id/sync`, ({ params }) => {
    const id = Number(params.id)
    return HttpResponse.json({
      id,
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
    const packageId = Number(params.id)
    const releases = mockReleases.filter((r) => r.package.id === packageId)
    return HttpResponse.json(releases)
  }),

  // GET /watchlist
  http.get(`${API_BASE}/watchlist`, () => {
    return HttpResponse.json(mockWatchlist)
  }),

  // PUT /watchlist
  http.put(`${API_BASE}/watchlist`, async ({ request }) => {
    const body = (await request.json()) as { packageIds: number[] }
    mockWatchlist.splice(0, mockWatchlist.length, ...body.packageIds)
    return HttpResponse.json(mockWatchlist)
  }),

  // POST /watchlist/:packageId
  http.post(`${API_BASE}/watchlist/:packageId`, ({ params }) => {
    const packageId = Number(params.packageId)
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
    const packageId = Number(params.packageId)
    const index = mockWatchlist.indexOf(packageId)
    if (index !== -1) {
      mockWatchlist.splice(index, 1)
    }
    return new HttpResponse(null, { status: 204 })
  }),

  // GET /notifications
  http.get(`${API_BASE}/notifications`, () => {
    return HttpResponse.json(mockNotifications)
  }),

  // GET /notifications/unread-count
  http.get(`${API_BASE}/notifications/unread-count`, () => {
    const count = mockNotifications.filter((n) => n.unread).length
    return HttpResponse.json({ count } satisfies UnreadCount)
  }),

  // PATCH /notifications/:id/read
  http.patch(`${API_BASE}/notifications/:id/read`, ({ params }) => {
    const id = Number(params.id)
    return HttpResponse.json({
      id,
      unread: false,
      lastReadAt: new Date().toISOString(),
    })
  }),

  // DELETE /notifications/:id
  http.delete(`${API_BASE}/notifications/:id`, () => {
    return new HttpResponse(null, { status: 204 })
  }),

  // GET /subscription/status
  http.get(`${API_BASE}/subscription/status`, () => {
    return HttpResponse.json({ isPro: false, status: null, expiresAt: null })
  }),
]
