import { render, screen, waitFor } from '../test/utils'
import userEvent from '@testing-library/user-event'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import {
  RouterProvider,
  createRouter,
  createRootRoute,
  createRoute,
  createMemoryHistory,
} from '@tanstack/react-router'
import { Home } from '../pages/Home'
import { Admin } from '../pages/Admin'

function createTestRouter(initialPath = '/') {
  const rootRoute = createRootRoute()

  const indexRoute = createRoute({
    getParentRoute: () => rootRoute,
    path: '/',
    component: Home,
  })

  const adminRoute = createRoute({
    getParentRoute: () => rootRoute,
    path: '/admin',
    component: Admin,
  })

  const routeTree = rootRoute.addChildren([indexRoute, adminRoute])

  const memoryHistory = createMemoryHistory({ initialEntries: [initialPath] })

  return createRouter({
    routeTree,
    history: memoryHistory,
  })
}

function renderWithRouter(initialPath = '/') {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false, gcTime: 0 },
      mutations: { retry: false },
    },
  })

  const router = createTestRouter(initialPath)

  return render(
    <QueryClientProvider client={queryClient}>
      <RouterProvider router={router} />
    </QueryClientProvider>
  )
}

describe('Routes', () => {
  describe('Home page (/) ', () => {
    it('renders home page at root path', async () => {
      renderWithRouter('/')

      await waitFor(() => {
        expect(screen.getByText('Patch Notes')).toBeInTheDocument()
      })
    })

    it('shows tracked packages section', async () => {
      renderWithRouter('/')

      await waitFor(() => {
        expect(screen.getByText('Tracked Packages')).toBeInTheDocument()
      })
    })

    it('shows recent releases section', async () => {
      renderWithRouter('/')

      await waitFor(() => {
        expect(screen.getByText('Recent Releases')).toBeInTheDocument()
      })
    })

    it('has admin link', async () => {
      renderWithRouter('/')

      await waitFor(() => {
        expect(screen.getByRole('link', { name: 'Admin' })).toBeInTheDocument()
      })
    })

    it('has add package button', async () => {
      renderWithRouter('/')

      await waitFor(() => {
        expect(
          screen.getByRole('button', { name: 'Add Package' })
        ).toBeInTheDocument()
      })
    })

    it('has settings button', async () => {
      renderWithRouter('/')

      await waitFor(() => {
        expect(
          screen.getByRole('button', { name: 'Settings' })
        ).toBeInTheDocument()
      })
    })
  })

  describe('Admin page (/admin)', () => {
    it('renders admin page at /admin path', async () => {
      renderWithRouter('/admin')

      await waitFor(() => {
        expect(screen.getByText('Package Management')).toBeInTheDocument()
      })
    })

    it('has back to home link', async () => {
      renderWithRouter('/admin')

      await waitFor(() => {
        expect(
          screen.getByRole('link', { name: 'Back to Home' })
        ).toBeInTheDocument()
      })
    })

    it('has add package button', async () => {
      renderWithRouter('/admin')

      await waitFor(() => {
        expect(
          screen.getByRole('button', { name: 'Add Package' })
        ).toBeInTheDocument()
      })
    })

    it('shows tracked packages table header', async () => {
      renderWithRouter('/admin')

      await waitFor(() => {
        expect(screen.getByText('Tracked Packages')).toBeInTheDocument()
        expect(
          screen.getByText(
            "Manage the npm packages you're tracking for release notes."
          )
        ).toBeInTheDocument()
      })
    })
  })

  describe('Navigation', () => {
    it('navigates from home to admin', async () => {
      const user = userEvent.setup()
      renderWithRouter('/')

      await waitFor(() => {
        expect(screen.getByText('Patch Notes')).toBeInTheDocument()
      })

      await user.click(screen.getByRole('link', { name: 'Admin' }))

      await waitFor(() => {
        expect(screen.getByText('Package Management')).toBeInTheDocument()
      })
    })

    it('navigates from admin to home', async () => {
      const user = userEvent.setup()
      renderWithRouter('/admin')

      await waitFor(() => {
        expect(screen.getByText('Package Management')).toBeInTheDocument()
      })

      await user.click(screen.getByRole('link', { name: 'Back to Home' }))

      await waitFor(() => {
        expect(screen.getByText('Patch Notes')).toBeInTheDocument()
      })
    })
  })
})

describe('Home page interactions', () => {
  it('opens add package form when button clicked', async () => {
    const user = userEvent.setup()
    renderWithRouter('/')

    await waitFor(() => {
      expect(screen.getByText('Patch Notes')).toBeInTheDocument()
    })

    await user.click(screen.getByRole('button', { name: 'Add Package' }))

    await waitFor(() => {
      expect(screen.getByText('Add New Package')).toBeInTheDocument()
      expect(
        screen.getByPlaceholderText('Package name (e.g., lodash)')
      ).toBeInTheDocument()
    })
  })

  it('opens settings modal when button clicked', async () => {
    const user = userEvent.setup()
    renderWithRouter('/')

    await waitFor(() => {
      expect(screen.getByText('Patch Notes')).toBeInTheDocument()
    })

    await user.click(screen.getByRole('button', { name: 'Settings' }))

    await waitFor(() => {
      expect(screen.getByRole('dialog')).toBeInTheDocument()
      expect(screen.getByText('Notifications')).toBeInTheDocument()
      expect(screen.getByText('Display')).toBeInTheDocument()
    })
  })

  it('displays loading state for packages initially', async () => {
    renderWithRouter('/')

    await waitFor(() => {
      expect(screen.getByText('Loading packages...')).toBeInTheDocument()
    })
  })

  it('displays loading state for releases initially', async () => {
    renderWithRouter('/')

    await waitFor(() => {
      expect(screen.getByText('Loading releases...')).toBeInTheDocument()
    })
  })
})

describe('Admin page interactions', () => {
  it('opens add package form when button clicked', async () => {
    const user = userEvent.setup()
    renderWithRouter('/admin')

    await waitFor(() => {
      expect(screen.getByText('Package Management')).toBeInTheDocument()
    })

    await user.click(screen.getByRole('button', { name: 'Add Package' }))

    await waitFor(() => {
      expect(screen.getByText('Add New Package')).toBeInTheDocument()
      expect(
        screen.getByPlaceholderText('Package name (e.g., lodash)')
      ).toBeInTheDocument()
    })
  })

  it('displays loading state for packages table initially', async () => {
    renderWithRouter('/admin')

    await waitFor(() => {
      expect(screen.getByText('Loading packages...')).toBeInTheDocument()
    })
  })
})
