import { createFileRoute } from '@tanstack/react-router'
import { Privacy } from '../pages/Privacy'
import { seoHead } from '../seo'

export const Route = createFileRoute('/privacy')({
  component: Privacy,
  head: () => ({
    ...seoHead({
      title: 'Privacy Policy | My Release Notes',
      description:
        'Read the My Release Notes privacy policy. Learn how we handle your data.',
      path: '/privacy',
    }),
  }),
})
