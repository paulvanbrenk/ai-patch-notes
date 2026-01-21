import { createFileRoute } from '@tanstack/react-router'
import { HomePageV2 } from '../pages/HomePageV2'

export const Route = createFileRoute('/preview')({
  component: HomePageV2,
})
