/* eslint-disable react-refresh/only-export-components */
import {
  createRootRouteWithContext,
  HeadContent,
  Outlet,
} from '@tanstack/react-router'
import type { QueryClient } from '@tanstack/react-query'
import { Footer } from '../components/ui/Footer'
import { seoHead } from '../seo'

function RootLayout() {
  return (
    <div className="min-h-screen flex flex-col">
      <HeadContent />
      <Outlet />
      <Footer />
    </div>
  )
}

export const Route = createRootRouteWithContext<{ queryClient: QueryClient }>()(
  {
    component: RootLayout,
    head: () => ({
      ...seoHead({
        title: 'My Release Notes - Track GitHub Releases | myreleasenotes.ai',
        description:
          'Track GitHub releases for the packages you depend on. AI-powered summaries, smart filtering, dark mode, and instant notifications. Free to start.',
        path: '/',
      }),
    }),
  }
)
