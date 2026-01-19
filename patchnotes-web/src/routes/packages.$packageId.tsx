import { createRoute } from '@tanstack/react-router'
import { rootRoute } from './__root'
import { PackageDetail } from '../pages/PackageDetail'

export const packageDetailRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '/packages/$packageId',
  component: function PackageDetailWrapper() {
    const { packageId } = packageDetailRoute.useParams()
    return <PackageDetail packageId={parseInt(packageId, 10)} />
  },
})
