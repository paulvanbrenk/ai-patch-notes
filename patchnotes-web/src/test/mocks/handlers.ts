import { http, HttpResponse } from 'msw'
import type {
  Package,
  Release,
  Notification,
  UnreadCount,
} from '../../api/types'

const API_BASE = '/api'

export const mockWatchlist: string[] = []

export const mockPackages: Package[] = [
  {
    id: 'pkg-react-test-id',
    npmName: 'react',
    githubOwner: 'facebook',
    githubRepo: 'react',
    lastFetchedAt: '2026-01-15T10:00:00Z',
    createdAt: '2026-01-01T00:00:00Z',
  },
  {
    id: 'pkg-lodash-test-id',
    npmName: 'lodash',
    githubOwner: 'lodash',
    githubRepo: 'lodash',
    lastFetchedAt: '2026-01-14T10:00:00Z',
    createdAt: '2026-01-02T00:00:00Z',
  },
]

export const mockReleases: Release[] = [
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

export const mockNotifications: Notification[] = [
  {
    id: 'notif-test-id-1',
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
      id: 'pkg-react-test-id',
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
    const id = params.id as string
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
      id: 'pkg-new-test-id',
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
    const body = (await request.json()) as Partial<Package>
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

  // GET /packages/:id/releases
  http.get(`${API_BASE}/packages/:id/releases`, ({ params }) => {
    const packageId = params.id as string
    const releases = mockReleases.filter((r) => r.package.id === packageId)
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
    const id = params.id as string
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
