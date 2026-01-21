import { createRootRoute, Outlet } from '@tanstack/react-router'
import { Footer } from '../components/ui/Footer'

function RootLayout() {
  return (
    <div className="min-h-screen flex flex-col">
      <Outlet />
      <Footer />
    </div>
  )
}

export const Route = createRootRoute({
  component: RootLayout,
})
