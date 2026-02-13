import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useStytchUser } from '@stytch/react'
import type { GetReleasesParams } from './generated/model'

import {
  useGetPackages,
  useGetPackage,
  useGetPackageReleases,
  getGetPackagesQueryKey,
  createPackage,
  deletePackage,
  updatePackage,
} from './generated/packages/packages'
import { useGetRelease, useGetReleases } from './generated/releases/releases'
import {
  useGetWatchlist,
  getGetWatchlistQueryKey,
  setWatchlist,
} from './generated/watchlist/watchlist'

import {
  GetPackagesResponse,
  GetPackageResponse,
  GetPackageReleasesResponse,
} from './generated/packages/packages.zod'
import {
  GetReleaseResponse,
  GetReleasesResponse,
} from './generated/releases/releases.zod'
import { GetWatchlistResponse } from './generated/watchlist/watchlist.zod'

// ── Query Hooks ──────────────────────────────────────────────

export function usePackages() {
  return useGetPackages({
    query: {
      select: (res) => GetPackagesResponse.parse(res.data),
    },
  })
}

export function usePackage(id: string) {
  return useGetPackage(id, {
    query: {
      select: (res) => GetPackageResponse.parse(res.data),
    },
  })
}

interface ReleasesOptions {
  packages?: string[]
  days?: number
  excludePrerelease?: boolean
  majorVersion?: number
}

export function useReleases(options?: ReleasesOptions) {
  const params: GetReleasesParams | undefined = options
    ? {
        packages: options.packages?.join(','),
        days: options.days,
        excludePrerelease: options.excludePrerelease,
        majorVersion: options.majorVersion,
      }
    : undefined

  return useGetReleases(params, {
    query: {
      select: (res) => GetReleasesResponse.parse(res.data),
    },
  })
}

export function useRelease(id: string) {
  return useGetRelease(id, {
    query: {
      select: (res) => GetReleaseResponse.parse(res.data),
    },
  })
}

export function usePackageReleases(packageId: string) {
  return useGetPackageReleases(packageId, {
    query: {
      select: (res) => GetPackageReleasesResponse.parse(res.data),
    },
  })
}

// ── Mutation Hooks ───────────────────────────────────────────

export function useAddPackage() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (npmName: string) => createPackage({ npmName }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: getGetPackagesQueryKey() })
    },
  })
}

export function useDeletePackage() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (id: string) => deletePackage(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: getGetPackagesQueryKey() })
    },
  })
}

export function useUpdatePackage() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({
      id,
      githubOwner,
      githubRepo,
    }: {
      id: string
      githubOwner?: string
      githubRepo?: string
    }) =>
      updatePackage(id, {
        githubOwner: githubOwner ?? null,
        githubRepo: githubRepo ?? null,
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: getGetPackagesQueryKey() })
    },
  })
}

// ── Watchlist Hooks ──────────────────────────────────────────

export function useWatchlist() {
  const { user } = useStytchUser()
  return useGetWatchlist({
    query: {
      enabled: !!user,
      select: (res) => GetWatchlistResponse.parse(res.data),
    },
  })
}

export function useSetWatchlist() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (packageIds: string[]) => setWatchlist({ packageIds }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: getGetWatchlistQueryKey() })
    },
  })
}
