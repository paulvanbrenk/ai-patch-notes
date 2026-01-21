import { StytchLogin } from '@stytch/react'
import { useStytchUser } from '@stytch/react'
import { Link, useNavigate } from '@tanstack/react-router'
import { useEffect } from 'react'
import { stytchLoginConfig } from '../auth/stytch'
import { Container } from '../components/ui'

export function Login() {
  const { user, isInitialized } = useStytchUser()
  const navigate = useNavigate()

  useEffect(() => {
    if (isInitialized && user) {
      navigate({ to: '/' })
    }
  }, [user, isInitialized, navigate])

  if (!isInitialized) {
    return (
      <Container>
        <div className="flex min-h-screen items-center justify-center">
          <p className="text-gray-600">Loading...</p>
        </div>
      </Container>
    )
  }

  if (user) {
    return null
  }

  return (
    <Container>
      <div className="flex min-h-screen flex-col items-center justify-center">
        <Link
          to="/"
          className="mb-6 text-text-secondary hover:text-text-primary"
        >
          ← Back to home
        </Link>
        <div className="mb-8 text-center">
          <h1 className="text-3xl font-bold text-gray-900">Welcome</h1>
          <p className="mt-2 text-gray-600">Sign in to continue</p>
        </div>
        <div className="w-full max-w-md">
          <StytchLogin config={stytchLoginConfig} />
        </div>
      </div>
    </Container>
  )
}
