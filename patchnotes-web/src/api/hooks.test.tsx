import { renderHook, waitFor } from '../test/utils'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import type { ReactNode } from 'react'
import { http, HttpResponse } from 'msw'
import { server } from '../test/mocks/server'
import {
  mockPackages,
  mockReleases,
  mockPackageReleases,
} from '../test/mocks/handlers'
import {
  usePackages,
  usePackage,
  useReleases,
  usePackageReleases,
  useAddPackage,
  useDeletePackage,
  useUpdatePackage,
} from './hooks'

function createWrapper() {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: {
        retry: false,
        gcTime: 0,
        staleTime: 0,
      },
      mutations: {
        retry: false,
      },
    },
  })

  return function Wrapper({ children }: { children: ReactNode }) {
    return (
      <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
    )
  }
}

describe('usePackages', () => {
  it('fetches packages successfully', async () => {
    const { result } = renderHook(() => usePackages(), {
      wrapper: createWrapper(),
    })

    await waitFor(() => expect(result.current.isSuccess).toBe(true))

    expect(result.current.data).toEqual(mockPackages)
  })

  it('handles error when fetching fails', async () => {
    server.use(
      http.get('/api/packages', () => {
        return new HttpResponse(null, { status: 500 })
      })
    )

    const { result } = renderHook(() => usePackages(), {
      wrapper: createWrapper(),
    })

    await waitFor(() => expect(result.current.isError).toBe(true))
  })
})

describe('usePackage', () => {
  it('fetches a single package successfully', async () => {
    const { result } = renderHook(() => usePackage('pkg-react-test-id'), {
      wrapper: createWrapper(),
    })

    await waitFor(() => expect(result.current.isSuccess).toBe(true))

    expect(result.current.data).toMatchObject({
      id: 'pkg-react-test-id',
      githubOwner: 'facebook',
      githubRepo: 'react',
    })
  })

  it('does not fetch when id is empty', async () => {
    const { result } = renderHook(() => usePackage(''), {
      wrapper: createWrapper(),
    })

    expect(result.current.isFetching).toBe(false)
    expect(result.current.data).toBeUndefined()
  })

  it('handles 404 error', async () => {
    server.use(
      http.get('/api/packages/nonexistent', () => {
        return new HttpResponse(null, { status: 404 })
      })
    )

    const { result } = renderHook(() => usePackage('nonexistent'), {
      wrapper: createWrapper(),
    })

    await waitFor(() => expect(result.current.isError).toBe(true))
  })
})

describe('useReleases', () => {
  it('fetches releases successfully', async () => {
    const { result } = renderHook(() => useReleases(), {
      wrapper: createWrapper(),
    })

    await waitFor(() => expect(result.current.isSuccess).toBe(true))

    expect(result.current.data).toEqual(mockReleases)
  })
})

describe('usePackageReleases', () => {
  it('fetches releases for a specific package', async () => {
    const { result } = renderHook(
      () => usePackageReleases('pkg-react-test-id'),
      {
        wrapper: createWrapper(),
      }
    )

    await waitFor(() => expect(result.current.isSuccess).toBe(true))

    expect(result.current.data).toEqual([mockPackageReleases[0]])
  })

  it('does not fetch when packageId is empty', async () => {
    const { result } = renderHook(() => usePackageReleases(''), {
      wrapper: createWrapper(),
    })

    expect(result.current.isFetching).toBe(false)
  })
})

describe('useAddPackage', () => {
  it('adds a new package successfully', async () => {
    const { result } = renderHook(() => useAddPackage(), {
      wrapper: createWrapper(),
    })

    result.current.mutate('axios')

    await waitFor(() => expect(result.current.isSuccess).toBe(true))

    expect(result.current.data).toMatchObject({
      data: { npmName: 'axios' },
    })
  })
})

describe('useDeletePackage', () => {
  it('deletes a package successfully', async () => {
    const { result } = renderHook(() => useDeletePackage(), {
      wrapper: createWrapper(),
    })

    result.current.mutate('pkg-react-test-id')

    await waitFor(() => expect(result.current.isSuccess).toBe(true))
  })
})

describe('useUpdatePackage', () => {
  it('updates a package successfully', async () => {
    const { result } = renderHook(() => useUpdatePackage(), {
      wrapper: createWrapper(),
    })

    result.current.mutate({
      id: 'pkg-react-test-id',
      githubOwner: 'new-owner',
    })

    await waitFor(() => expect(result.current.isSuccess).toBe(true))

    expect(result.current.data).toMatchObject({
      data: { githubOwner: 'new-owner' },
    })
  })
})
