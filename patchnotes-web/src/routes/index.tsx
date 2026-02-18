import { createFileRoute } from '@tanstack/react-router'
import { HomePage } from '../pages/HomePage'
import { seoHead } from '../seo'

export const Route = createFileRoute('/')({
  component: HomePage,
  head: () => ({
    ...seoHead({
      title: 'My Release Notes - Track GitHub Releases | myreleasenotes.ai',
      description:
        'Track GitHub releases for the packages you depend on. AI-powered summaries, smart filtering, dark mode, and instant notifications. Free to start.',
      path: '/',
    }),
  }),
})
