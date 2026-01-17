/* eslint-disable react-refresh/only-export-components */
import { createRootRoute, Outlet } from '@tanstack/react-router'

function RootLayout() {
  return <Outlet />
}

export const rootRoute = createRootRoute({
  component: RootLayout,
})
