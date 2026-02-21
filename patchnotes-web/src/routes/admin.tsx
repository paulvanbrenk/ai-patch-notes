import { createFileRoute } from '@tanstack/react-router'
import { seoHead } from '../seo'

export const Route = createFileRoute('/admin')({
  head: () => ({
    ...seoHead({
      title: 'Admin | My Release Notes',
      description: 'Admin dashboard for My Release Notes.',
      path: '/admin',
      noindex: true,
    }),
  }),
})
