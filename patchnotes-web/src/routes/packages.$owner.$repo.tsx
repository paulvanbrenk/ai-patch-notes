import { createFileRoute } from '@tanstack/react-router'
import { PackageDetailByRepo } from '../pages/PackageDetailByRepo'
import { seoHead } from '../seo'
import { getGetPackageByOwnerRepoQueryOptions } from '../api/generated/packages/packages'

export const Route = createFileRoute('/packages/$owner/$repo')({
  loader: ({ context, params }) =>
    context.queryClient.ensureQueryData(
      getGetPackageByOwnerRepoQueryOptions(params.owner, params.repo)
    ),
  component: function PackageDetailByRepoWrapper() {
    const { owner, repo } = Route.useParams()
    return <PackageDetailByRepo owner={owner} repo={repo} />
  },
  head: ({ params }) => ({
    ...seoHead({
      title: `${params.owner}/${params.repo} | My Release Notes`,
      description: `Track GitHub releases for ${params.owner}/${params.repo}. AI-powered summaries and notifications.`,
      path: `/packages/${params.owner}/${params.repo}`,
    }),
  }),
})
