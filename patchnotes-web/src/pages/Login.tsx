import { StytchLogin } from '@stytch/react'
import { useStytchUser } from '@stytch/react'
import { Link, useNavigate } from '@tanstack/react-router'
import { useEffect, useMemo } from 'react'
import { FileText, ArrowLeft } from 'lucide-react'
import { stytchLoginConfig, getStytchStyles } from '../auth/stytch'
import { useTheme } from '../components/theme'
import { ThemeToggle } from '../components/theme'

export function Login() {
  const { user, isInitialized } = useStytchUser()
  const navigate = useNavigate()
  const { resolvedTheme } = useTheme()

  const stytchStyles = useMemo(
    () => getStytchStyles(resolvedTheme === 'dark'),
    [resolvedTheme]
  )

  useEffect(() => {
    if (isInitialized && user) {
      navigate({ to: '/' })
    }
  }, [user, isInitialized, navigate])

  if (!isInitialized) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-surface-secondary">
        <div className="animate-pulse text-text-tertiary">Loading...</div>
      </div>
    )
  }

  if (user) {
    return null
  }

  return (
    <div className="min-h-screen bg-surface-secondary">
      {/* Subtle gradient overlay */}
      <div
        className="fixed inset-0 pointer-events-none opacity-40 dark:opacity-20"
        style={{
          background:
            resolvedTheme === 'dark'
              ? 'radial-gradient(ellipse at top, rgba(99, 102, 241, 0.15) 0%, transparent 50%)'
              : 'radial-gradient(ellipse at top, rgba(79, 70, 229, 0.08) 0%, transparent 50%)',
        }}
      />

      {/* Header */}
      <header className="relative z-10 flex items-center justify-between px-6 py-4">
        <Link
          to="/"
          className="flex items-center gap-2 text-text-secondary hover:text-text-primary transition-colors"
        >
          <ArrowLeft className="w-4 h-4" />
          <span className="text-sm font-medium">Back</span>
        </Link>
        <ThemeToggle />
      </header>

      {/* Main content */}
      <main className="relative z-10 flex flex-col items-center justify-center px-4 pt-12 pb-24">
        {/* Logo/Brand */}
        <div className="mb-8 text-center">
          <div className="flex items-center justify-center gap-3 mb-2">
            <div className="flex items-center justify-center w-10 h-10 rounded-lg bg-gradient-to-br from-brand-500 to-brand-600 shadow-lg shadow-brand-500/25">
              <FileText className="w-5 h-5 text-white" strokeWidth={1.5} />
            </div>
            <h1 className="text-2xl font-semibold text-text-primary tracking-tight">
              Patch Notes
            </h1>
          </div>
          <p className="text-text-tertiary text-sm">
            AI-powered release summaries
          </p>
        </div>

        {/* Login card */}
        <div className="w-full max-w-sm">
          <div
            className="
              bg-surface-primary
              border border-border-default
              rounded-lg
              shadow-xl shadow-black/5
              dark:shadow-black/20
              overflow-hidden
              p-6
            "
          >
            <StytchLogin config={stytchLoginConfig} styles={stytchStyles} />
          </div>

          {/* Footer text */}
          <p className="mt-6 text-center text-xs text-text-tertiary">
            By continuing, you agree to our terms of service
          </p>
        </div>
      </main>
    </div>
  )
}
