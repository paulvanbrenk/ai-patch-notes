import { createLazyFileRoute } from '@tanstack/react-router'
import { Admin } from '../pages/Admin'

export const Route = createLazyFileRoute('/admin')({
  component: Admin,
})
