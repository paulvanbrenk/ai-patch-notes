/* eslint-disable react-refresh/only-export-components */
import { createFileRoute } from '@tanstack/react-router'
import { PackageDetailByRepo } from '../pages/PackageDetailByRepo'

export const Route = createFileRoute('/packages/$owner/$repo')({
  component: function PackageDetailByRepoWrapper() {
    const { owner, repo } = Route.useParams()
    return <PackageDetailByRepo owner={owner} repo={repo} />
  },
})
