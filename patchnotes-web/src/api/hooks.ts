import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { api } from './client'
import type { Package, Release, Notification, UnreadCount } from './types'

export const queryKeys = {
  packages: ['packages'] as const,
  package: (id: number) => ['packages', id] as const,
  releases: ['releases'] as const,
  packageReleases: (packageId: number) =>
    ['packages', packageId, 'releases'] as const,
  notifications: ['notifications'] as const,
  notificationsUnreadCount: ['notifications', 'unread-count'] as const,
}

export function usePackages() {
  return useQuery({
    queryKey: queryKeys.packages,
    queryFn: () => api.get<Package[]>('/packages'),
  })
}

export function usePackage(id: number) {
  return useQuery({
    queryKey: queryKeys.package(id),
    queryFn: () => api.get<Package>(`/packages/${id}`),
    enabled: id > 0,
  })
}

export function useReleases() {
  return useQuery({
    queryKey: queryKeys.releases,
    queryFn: () => api.get<Release[]>('/releases'),
  })
}

export function usePackageReleases(packageId: number) {
  return useQuery({
    queryKey: queryKeys.packageReleases(packageId),
    queryFn: () => api.get<Release[]>(`/packages/${packageId}/releases`),
    enabled: packageId > 0,
  })
}

export function useNotifications(options?: { unreadOnly?: boolean; packageId?: number }) {
  const params = new URLSearchParams()
  if (options?.unreadOnly) {
    params.set('unreadOnly', 'true')
  }
  if (options?.packageId) {
    params.set('packageId', options.packageId.toString())
  }
  const queryString = params.toString()
  const endpoint = queryString ? `/notifications?${queryString}` : '/notifications'

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
    mutationFn: (id: number) => api.patch<{ id: number; unread: boolean; lastReadAt: string }>(`/notifications/${id}/read`),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.notifications })
      queryClient.invalidateQueries({ queryKey: queryKeys.notificationsUnreadCount })
    },
  })
}

export function useDeleteNotification() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (id: number) => api.delete(`/notifications/${id}`),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.notifications })
      queryClient.invalidateQueries({ queryKey: queryKeys.notificationsUnreadCount })
    },
  })
}
