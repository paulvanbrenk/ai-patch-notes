import { createFileRoute } from '@tanstack/react-router'
import { PreviewPage } from '../pages/PreviewPage'

export const Route = createFileRoute('/preview')({
  component: PreviewPage,
})
