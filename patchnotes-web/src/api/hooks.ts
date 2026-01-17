import { useQuery } from '@tanstack/react-query'
import { api } from './client'
import type { Package, Release } from './types'

export const queryKeys = {
  packages: ['packages'] as const,
  package: (id: number) => ['packages', id] as const,
  releases: ['releases'] as const,
  packageReleases: (packageId: number) =>
    ['packages', packageId, 'releases'] as const,
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
