import { createFileRoute } from '@tanstack/react-router'
import { Admin } from '../pages/Admin'
import { seoHead } from '../seo'

export const Route = createFileRoute('/admin')({
  component: Admin,
  head: () => ({
    ...seoHead({
      title: 'Admin | My Release Notes',
      description: 'Admin dashboard for My Release Notes.',
      path: '/admin',
      noindex: true,
    }),
  }),
})
