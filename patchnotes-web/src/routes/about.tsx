import { createFileRoute } from '@tanstack/react-router'
import { About } from '../pages/About'
import { seoHead } from '../seo'

export const Route = createFileRoute('/about')({
  component: About,
  head: () => ({
    ...seoHead({
      title: 'About | My Release Notes',
      description:
        'Learn about My Release Notes — the easiest way to track GitHub releases for the packages you depend on.',
      path: '/about',
    }),
  }),
})
