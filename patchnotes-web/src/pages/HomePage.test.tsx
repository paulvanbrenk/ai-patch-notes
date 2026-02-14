import { render, screen, waitFor } from '../test/utils'
import { http, HttpResponse } from 'msw'
import { server } from '../test/mocks/server'
import { mockFeedGroups } from '../test/mocks/handlers'
import { HomePage } from './HomePage'

// Mock @tanstack/react-router Link to avoid RouterProvider requirement
vi.mock('@tanstack/react-router', () => ({
  Link: ({ children, to, ...props }: Record<string, unknown>) => (
    <a href={to as string} {...props}>
      {children as React.ReactNode}
    </a>
  ),
  useNavigate: () => vi.fn(),
}))

// Override the global @stytch/react mock so we can control auth state per-test
const mockStytchUser = vi.hoisted(() => vi.fn())

vi.mock('@stytch/react', () => ({
  StytchProvider: ({ children }: { children: React.ReactNode }) => children,
  useStytch: () => ({
    magicLinks: { authenticate: vi.fn() },
    oauth: { authenticate: vi.fn() },
    session: { revoke: vi.fn() },
  }),
  useStytchUser: mockStytchUser,
}))

const authenticatedUser = {
  user: {
    user_id: 'test-user-id',
    emails: [{ email: 'test@example.com' }],
    roles: ['patch_notes_admin'],
  },
  isInitialized: true,
}

const anonymousUser = {
  user: null,
  isInitialized: true,
}

beforeEach(() => {
  localStorage.clear()
})

describe('HomePage', () => {
  describe('anonymous user', () => {
    beforeEach(() => {
      mockStytchUser.mockReturnValue(anonymousUser)
    })

    it('sees releases with default backend filtering', async () => {
      render(<HomePage />)

      await waitFor(() => {
        expect(screen.getByText('react')).toBeInTheDocument()
      })
      expect(screen.getByText('lodash')).toBeInTheDocument()
    })

    it('sees the hero card', async () => {
      render(<HomePage />)

      await waitFor(() => {
        expect(
          screen.getByText(/Never miss a release that matters/)
        ).toBeInTheDocument()
      })
    })

    it('does not see the empty watchlist prompt', async () => {
      render(<HomePage />)

      await waitFor(() => {
        expect(
          screen.getByText(/Never miss a release that matters/)
        ).toBeInTheDocument()
      })
      expect(
        screen.queryByText(/Add packages to your watchlist/)
      ).not.toBeInTheDocument()
    })
  })

  describe('authenticated user with watchlist', () => {
    beforeEach(() => {
      mockStytchUser.mockReturnValue(authenticatedUser)
    })

    it('sees releases filtered by watchlist', async () => {
      // Watchlist contains only React — feed endpoint handles filtering server-side
      server.use(
        http.get('/api/watchlist', () => {
          return HttpResponse.json(['pkg-react-test-id'])
        }),
        http.get('/api/feed', () => {
          // Server returns only React groups for this user's watchlist
          return HttpResponse.json({
            groups: mockFeedGroups.filter(
              (g) => g.packageId === 'pkg-react-test-id'
            ),
          })
        })
      )

      render(<HomePage />)

      await waitFor(() => {
        expect(screen.getAllByText('react').length).toBeGreaterThan(0)
      })
      // Lodash releases should not appear since it's not in the watchlist
      // (lodash still appears in the PackagePicker sidebar)
      expect(screen.queryByText('v4.x')).not.toBeInTheDocument()
    })

    it('does not show hero card', async () => {
      server.use(
        http.get('/api/watchlist', () => {
          return HttpResponse.json(['pkg-react-test-id'])
        })
      )

      render(<HomePage />)

      await waitFor(() => {
        expect(screen.getAllByText('react').length).toBeGreaterThan(0)
      })
      expect(
        screen.queryByText(/Never miss a release that matters/)
      ).not.toBeInTheDocument()
    })
  })

  describe('authenticated user with empty watchlist', () => {
    beforeEach(() => {
      mockStytchUser.mockReturnValue(authenticatedUser)
      server.use(
        http.get('/api/watchlist', () => {
          return HttpResponse.json([])
        })
      )
    })

    it('sees prompt to add packages', async () => {
      render(<HomePage />)

      await waitFor(() => {
        expect(
          screen.getByText(/Add packages to your watchlist/)
        ).toBeInTheDocument()
      })
    })

    it('does not show hero card', async () => {
      render(<HomePage />)

      await waitFor(() => {
        expect(
          screen.getByText(/Add packages to your watchlist/)
        ).toBeInTheDocument()
      })
      expect(
        screen.queryByText(/Never miss a release that matters/)
      ).not.toBeInTheDocument()
    })
  })
})
