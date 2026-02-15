import { createFileRoute } from '@tanstack/react-router'
import { AdminEmails } from '../pages/AdminEmails'

export const Route = createFileRoute('/admin_/emails')({
  component: AdminEmails,
})
