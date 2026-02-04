import { createFileRoute } from '@tanstack/react-router'
import { PackageDetail } from '../pages/PackageDetail'

export const Route = createFileRoute('/packages/$packageId')({
  component: function PackageDetailWrapper() {
    const { packageId } = Route.useParams()
    return <PackageDetail packageId={packageId} />
  },
})
