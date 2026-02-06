import { useStytch, useStytchUser } from '@stytch/react'
import { Link } from '@tanstack/react-router'
import { Button } from '../ui'

export function UserMenu() {
  const stytch = useStytch()
  const { user, isInitialized } = useStytchUser()

  const handleLogout = async () => {
    await stytch.session.revoke()
  }

  if (!isInitialized) {
    return (
      <div className="h-9 w-20 animate-pulse rounded-md bg-surface-secondary" />
    )
  }

  if (!user) {
    return (
      <Link to="/login">
        <Button variant="primary" size="sm">
          Sign In
        </Button>
      </Link>
    )
  }

  const email = user.emails?.[0]?.email
  const displayName = email?.split('@')[0] || 'User'

  return (
    <div className="flex items-center gap-3">
      <span className="text-sm text-text-secondary">{displayName}</span>
      <Button variant="secondary" size="sm" onClick={handleLogout}>
        Sign Out
      </Button>
    </div>
  )
}
