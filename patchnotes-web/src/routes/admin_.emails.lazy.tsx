/* eslint-disable react-refresh/only-export-components */
import { createLazyFileRoute } from '@tanstack/react-router'
import { AdminEmails } from '../pages/AdminEmails'

export const Route = createLazyFileRoute('/admin_/emails')({
  component: AdminEmails,
})
