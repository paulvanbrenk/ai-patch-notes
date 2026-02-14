/* eslint-disable react-refresh/only-export-components */
import { createFileRoute } from '@tanstack/react-router'
import { OwnerPackagesPage } from '../pages/OwnerPackagesPage'

export const Route = createFileRoute('/packages/$owner/')({
  component: function OwnerPageWrapper() {
    const { owner } = Route.useParams()
    return <OwnerPackagesPage owner={owner} />
  },
})
