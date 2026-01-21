import { createFileRoute } from '@tanstack/react-router'
import { ReleaseDetail } from '../pages/ReleaseDetail'

export const Route = createFileRoute('/releases/$releaseId')({
  component: function ReleaseDetailWrapper() {
    const { releaseId } = Route.useParams()
    return <ReleaseDetail releaseId={parseInt(releaseId, 10)} />
  },
})
