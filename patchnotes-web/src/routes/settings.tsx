import { createFileRoute } from '@tanstack/react-router'
import { Settings } from '../pages/Settings'
import { seoHead } from '../seo'

export const Route = createFileRoute('/settings')({
  component: Settings,
  head: () => ({
    ...seoHead({
      title: 'Settings | My Release Notes',
      description: 'Manage your My Release Notes account settings.',
      path: '/settings',
      noindex: true,
    }),
  }),
})
