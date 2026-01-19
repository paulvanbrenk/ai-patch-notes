import { createRoute } from '@tanstack/react-router'
import { rootRoute } from './__root'
import { ReleaseDetail } from '../pages/ReleaseDetail'

export const releaseDetailRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '/releases/$releaseId',
  component: function ReleaseDetailWrapper() {
    const { releaseId } = releaseDetailRoute.useParams()
    return <ReleaseDetail releaseId={parseInt(releaseId, 10)} />
  },
})
