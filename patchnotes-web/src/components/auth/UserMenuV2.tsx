import { useState, useRef, useEffect } from 'react'
import { useStytch, useStytchUser } from '@stytch/react'
import { Link } from '@tanstack/react-router'
import { Settings, HelpCircle, LogOut } from 'lucide-react'

// ============================================================================
// Utilities
// ============================================================================

function getInitials(email?: string, name?: string): string {
  if (name) {
    const parts = name.split(' ')
    if (parts.length >= 2) {
      return `${parts[0][0]}${parts[1][0]}`.toUpperCase()
    }
    return name.slice(0, 2).toUpperCase()
  }
  if (email) {
    const local = email.split('@')[0]
    return local.slice(0, 2).toUpperCase()
  }
  return '?'
}

function getAvatarColor(email?: string): { bg: string; text: string } {
  // Generate consistent color from email
  const colors = [
    { bg: 'bg-amber-500', text: 'text-amber-50' },
    { bg: 'bg-emerald-500', text: 'text-emerald-50' },
    { bg: 'bg-sky-500', text: 'text-sky-50' },
    { bg: 'bg-violet-500', text: 'text-violet-50' },
    { bg: 'bg-rose-500', text: 'text-rose-50' },
    { bg: 'bg-cyan-500', text: 'text-cyan-50' },
    { bg: 'bg-orange-500', text: 'text-orange-50' },
    { bg: 'bg-indigo-500', text: 'text-indigo-50' },
  ]

  if (!email) return colors[0]

  let hash = 0
  for (let i = 0; i < email.length; i++) {
    hash = email.charCodeAt(i) + ((hash << 5) - hash)
  }
  return colors[Math.abs(hash) % colors.length]
}

// ============================================================================
// Avatar Component
// ============================================================================

function Avatar({
  email,
  name,
  size = 'md',
}: {
  email?: string
  name?: string
  size?: 'sm' | 'md' | 'lg'
}) {
  const initials = getInitials(email, name)
  const { bg, text } = getAvatarColor(email)

  const sizeClasses = {
    sm: 'h-7 w-7 text-xs',
    md: 'h-9 w-9 text-sm',
    lg: 'h-11 w-11 text-base',
  }

  return (
    <div
      className={`${sizeClasses[size]} ${bg} ${text} rounded-full flex items-center justify-center font-semibold select-none`}
    >
      {initials}
    </div>
  )
}

// ============================================================================
// Dropdown Menu
// ============================================================================

function DropdownMenu({
  isOpen,
  onClose,
  email,
  displayName,
  onLogout,
}: {
  isOpen: boolean
  onClose: () => void
  email?: string
  displayName: string
  onLogout: () => void
}) {
  const menuRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    if (!isOpen) return

    const handleClickOutside = (e: MouseEvent) => {
      if (menuRef.current && !menuRef.current.contains(e.target as Node)) {
        onClose()
      }
    }

    const handleEscape = (e: KeyboardEvent) => {
      if (e.key === 'Escape') onClose()
    }

    document.addEventListener('mousedown', handleClickOutside)
    document.addEventListener('keydown', handleEscape)
    return () => {
      document.removeEventListener('mousedown', handleClickOutside)
      document.removeEventListener('keydown', handleEscape)
    }
  }, [isOpen, onClose])

  if (!isOpen) return null

  return (
    <div
      ref={menuRef}
      className="absolute right-0 top-full mt-2 w-64 origin-top-right
        rounded-xl bg-surface-primary border border-border-default
        shadow-lg ring-1 ring-black/5
        animate-in fade-in slide-in-from-top-2 duration-150"
      role="menu"
    >
      {/* User info section */}
      <div className="px-4 py-3 border-b border-border-muted">
        <div className="flex items-center gap-3">
          <Avatar email={email} size="lg" />
          <div className="min-w-0 flex-1">
            <p className="text-sm font-medium text-text-primary truncate">
              {displayName}
            </p>
            {email && (
              <p className="text-xs text-text-tertiary truncate">{email}</p>
            )}
          </div>
        </div>
      </div>

      {/* Menu items */}
      <div className="py-1.5">
        <MenuButton icon={<Settings className="w-4 h-4" />} onClick={() => {}}>
          Settings
        </MenuButton>
        <MenuButton
          icon={<HelpCircle className="w-4 h-4" />}
          onClick={() => {}}
        >
          Help & Support
        </MenuButton>
      </div>

      {/* Sign out section */}
      <div className="border-t border-border-muted py-1.5">
        <MenuButton
          icon={<LogOut className="w-4 h-4" />}
          onClick={onLogout}
          variant="danger"
        >
          Sign out
        </MenuButton>
      </div>
    </div>
  )
}

function MenuButton({
  children,
  icon,
  onClick,
  variant = 'default',
}: {
  children: React.ReactNode
  icon: React.ReactNode
  onClick: () => void
  variant?: 'default' | 'danger'
}) {
  return (
    <button
      onClick={onClick}
      className={`w-full flex items-center gap-3 px-4 py-2 text-sm transition-colors
        ${
          variant === 'danger'
            ? 'text-red-600 hover:bg-red-50 dark:text-red-400 dark:hover:bg-red-950/30'
            : 'text-text-primary hover:bg-surface-tertiary'
        }`}
      role="menuitem"
    >
      <span className="w-4 h-4 opacity-60">{icon}</span>
      {children}
    </button>
  )
}

// ============================================================================
// Main Component
// ============================================================================

export function UserMenuV2() {
  const stytch = useStytch()
  const { user, isInitialized } = useStytchUser()
  const [isOpen, setIsOpen] = useState(false)

  const handleLogout = async () => {
    setIsOpen(false)
    await stytch.session.revoke()
  }

  // Loading state
  if (!isInitialized) {
    return (
      <div className="h-9 w-9 animate-pulse rounded-full bg-surface-tertiary" />
    )
  }

  // Signed out state
  if (!user) {
    return (
      <Link
        to="/login"
        className="flex items-center gap-2 rounded-lg px-3 py-2 text-sm font-medium
          text-text-primary bg-surface-primary border border-border-default
          hover:border-border-default hover:bg-surface-tertiary
          transition-colors duration-150"
      >
        Sign in
      </Link>
    )
  }

  // Signed in state
  const email = user.emails?.[0]?.email
  const displayName = user.name?.first_name || email?.split('@')[0] || 'User'

  return (
    <div className="relative">
      <button
        onClick={() => setIsOpen(!isOpen)}
        className="flex items-center rounded-full
          ring-2 ring-transparent hover:ring-border-default
          focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-500
          transition-all duration-150"
        aria-expanded={isOpen}
        aria-haspopup="true"
      >
        <Avatar email={email} />
      </button>

      <DropdownMenu
        isOpen={isOpen}
        onClose={() => setIsOpen(false)}
        email={email}
        displayName={displayName}
        onLogout={handleLogout}
      />
    </div>
  )
}
