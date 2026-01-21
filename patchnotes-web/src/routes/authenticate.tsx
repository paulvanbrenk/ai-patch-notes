import { createFileRoute } from '@tanstack/react-router'
import { Authenticate } from '../pages/Authenticate'

export const Route = createFileRoute('/authenticate')({
  component: Authenticate,
})
