import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { api } from './client'
import type {
  Package,
  Release,
  AddPackageRequest,
  AddPackageResponse,
  UpdatePackageRequest,
  Notification,
  UnreadCount,
} from './types'

export const queryKeys = {
  packages: ['packages'] as const,
  package: (id: string) => ['packages', id] as const,
  releases: ['releases'] as const,
  release: (id: string) => ['releases', id] as const,
  packageReleases: (packageId: string) =>
    ['packages', packageId, 'releases'] as const,
  notifications: ['notifications'] as const,
  notificationsUnreadCount: ['notifications', 'unread-count'] as const,
  watchlist: ['watchlist'] as const,
}

export function usePackages() {
  return useQuery({
    queryKey: queryKeys.packages,
    queryFn: () => api.get<Package[]>('/packages'),
  })
}

export function usePackage(id: string) {
  return useQuery({
    queryKey: queryKeys.package(id),
    queryFn: () => api.get<Package>(`/packages/${id}`),
    enabled: !!id,
  })
}

export interface ReleasesOptions {
  packages?: string[]
  days?: number
  excludePrerelease?: boolean
  majorVersion?: number
}

export function useReleases(options?: ReleasesOptions) {
  const params = new URLSearchParams()
  if (options?.packages?.length) {
    params.set('packages', options.packages.join(','))
  }
  if (options?.days) {
    params.set('days', options.days.toString())
  }
  if (options?.excludePrerelease) {
    params.set('excludePrerelease', 'true')
  }
  if (options?.majorVersion !== undefined) {
    params.set('majorVersion', options.majorVersion.toString())
  }
  const queryString = params.toString()
  const endpoint = queryString ? `/releases?${queryString}` : '/releases'

  return useQuery({
    queryKey: [...queryKeys.releases, options],
    queryFn: () => api.get<Release[]>(endpoint),
  })
}

export function useRelease(id: string) {
  return useQuery({
    queryKey: queryKeys.release(id),
    queryFn: () => api.get<Release>(`/releases/${id}`),
    enabled: !!id,
  })
}

export function usePackageReleases(packageId: string) {
  return useQuery({
    queryKey: queryKeys.packageReleases(packageId),
    queryFn: () => api.get<Release[]>(`/packages/${packageId}/releases`),
    enabled: !!packageId,
  })
}

export function useAddPackage() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (npmName: string) =>
      api.post<AddPackageResponse>('/packages', {
        npmName,
      } satisfies AddPackageRequest),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.packages })
    },
  })
}

export function useDeletePackage() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (id: string) => api.delete(`/packages/${id}`),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.packages })
    },
  })
}

export function useUpdatePackage() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ id, ...data }: UpdatePackageRequest & { id: string }) =>
      api.patch<Package>(`/packages/${id}`, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.packages })
    },
  })
}

export function useNotifications(options?: {
  unreadOnly?: boolean
  packageId?: string
}) {
  const params = new URLSearchParams()
  if (options?.unreadOnly) {
    params.set('unreadOnly', 'true')
  }
  if (options?.packageId) {
    params.set('packageId', options.packageId)
  }
  const queryString = params.toString()
  const endpoint = queryString
    ? `/notifications?${queryString}`
    : '/notifications'

  return useQuery({
    queryKey: [...queryKeys.notifications, options],
    queryFn: () => api.get<Notification[]>(endpoint),
  })
}

export function useNotificationsUnreadCount() {
  return useQuery({
    queryKey: queryKeys.notificationsUnreadCount,
    queryFn: () => api.get<UnreadCount>('/notifications/unread-count'),
  })
}

export function useMarkNotificationAsRead() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (id: string) =>
      api.patch<{ id: string; unread: boolean; lastReadAt: string }>(
        `/notifications/${id}/read`
      ),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.notifications })
      queryClient.invalidateQueries({
        queryKey: queryKeys.notificationsUnreadCount,
      })
    },
  })
}

export function useDeleteNotification() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (id: string) => api.delete(`/notifications/${id}`),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.notifications })
      queryClient.invalidateQueries({
        queryKey: queryKeys.notificationsUnreadCount,
      })
    },
  })
}

export function useWatchlist() {
  return useQuery({
    queryKey: queryKeys.watchlist,
    queryFn: () => api.get<string[]>('/watchlist'),
  })
}

export function useSetWatchlist() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (packageIds: string[]) =>
      api.put<string[]>('/watchlist', { packageIds }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.watchlist })
    },
  })
}

export function useAddToWatchlist() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (packageId: string) =>
      api.post<string>(`/watchlist/${packageId}`),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.watchlist })
    },
  })
}

export function useRemoveFromWatchlist() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (packageId: string) => api.delete(`/watchlist/${packageId}`),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.watchlist })
    },
  })
}
