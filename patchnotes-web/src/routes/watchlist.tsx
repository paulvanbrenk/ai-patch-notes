import { createFileRoute } from '@tanstack/react-router'
import { WatchlistPage } from '../pages/WatchlistPage'

export const Route = createFileRoute('/watchlist')({
  component: WatchlistPage,
})
