import { createFileRoute } from '@tanstack/react-router'
import { WatchlistPage } from '../pages/WatchlistPage'
import { seoHead } from '../seo'

export const Route = createFileRoute('/watchlist')({
  component: WatchlistPage,
  head: () => ({
    ...seoHead({
      title: 'Watchlist | My Release Notes',
      description: 'Your personal watchlist of GitHub packages and releases.',
      path: '/watchlist',
      noindex: true,
    }),
  }),
})
