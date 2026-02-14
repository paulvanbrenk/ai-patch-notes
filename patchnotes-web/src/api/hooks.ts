import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useStytchUser } from '@stytch/react'
import * as z from 'zod'
import type { GetReleasesParams } from './generated/model'
import { api } from './client'

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

// ── Helpers ─────────────────────────────────────────────────

function safeParse<T extends z.ZodType>(
  schema: T,
  data: unknown
): z.output<T> | null {
  const result = schema.safeParse(data)
  if (!result.success) {
    console.error('[Zod validation error]', z.prettifyError(result.error))
    return null
  }
  return result.data
}

// ── Query Hooks ──────────────────────────────────────────────

export function usePackages() {
  return useGetPackages({
    query: {
      select: (res) => safeParse(GetPackagesResponse, res.data),
    },
  })
}

export function usePackage(id: string) {
  return useGetPackage(id, {
    query: {
      select: (res) => safeParse(GetPackageResponse, res.data),
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
      select: (res) => safeParse(GetReleasesResponse, res.data),
    },
  })
}

export function useRelease(id: string) {
  return useGetRelease(id, {
    query: {
      select: (res) => safeParse(GetReleaseResponse, res.data),
    },
  })
}

export function usePackageReleases(packageId: string) {
  return useGetPackageReleases(packageId, {
    query: {
      select: (res) => safeParse(GetPackageReleasesResponse, res.data),
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
      select: (res) => safeParse(GetWatchlistResponse, res.data),
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

// ── Feed Types ──────────────────────────────────────────────

export interface FeedReleaseDto {
  id: string
  tag: string
  title?: string | null
  publishedAt: string
}

export interface FeedGroupDto {
  packageId: string
  packageName: string
  npmName?: string | null
  githubOwner: string
  githubRepo: string
  majorVersion: number
  isPrerelease: boolean
  versionRange: string
  summary?: string | null
  releaseCount: number
  lastUpdated: string
  releases: FeedReleaseDto[]
}

export interface FeedResponseDto {
  groups: FeedGroupDto[]
}

// ── Feed Hook ───────────────────────────────────────────────

interface FeedOptions {
  excludePrerelease?: boolean
}

export function useFeed(options?: FeedOptions) {
  const params = new URLSearchParams()
  if (options?.excludePrerelease) {
    params.set('excludePrerelease', 'true')
  }
  const qs = params.toString()
  const url = qs ? `/feed?${qs}` : '/feed'

  return useQuery({
    queryKey: ['/api/feed', options] as const,
    queryFn: () => api.get<FeedResponseDto>(url),
  })
}
