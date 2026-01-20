import { useStytch, useStytchUser } from '@stytch/react'
import { useNavigate } from '@tanstack/react-router'
import { useEffect, useState } from 'react'
import { Container } from '../components/ui'

export function Authenticate() {
  const stytch = useStytch()
  const { user, isInitialized } = useStytchUser()
  const navigate = useNavigate()
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    if (!isInitialized) return

    if (user) {
      navigate({ to: '/' })
      return
    }

    const params = new URLSearchParams(window.location.search)
    const token = params.get('token')
    const tokenType = params.get('stytch_token_type')

    if (!token || !tokenType) {
      setError('Invalid authentication link')
      return
    }

    const authenticate = async () => {
      try {
        if (tokenType === 'magic_links') {
          await stytch.magicLinks.authenticate(token, {
            session_duration_minutes: 60,
          })
        } else if (tokenType === 'oauth') {
          await stytch.oauth.authenticate(token, {
            session_duration_minutes: 60,
          })
        } else {
          setError(`Unknown token type: ${tokenType}`)
          return
        }
        navigate({ to: '/' })
      } catch (err) {
        console.error('Authentication failed:', err)
        setError('Authentication failed. Please try again.')
      }
    }

    authenticate()
  }, [stytch, user, isInitialized, navigate])

  if (error) {
    return (
      <Container>
        <div className="flex min-h-screen flex-col items-center justify-center">
          <div className="text-center">
            <h1 className="text-2xl font-bold text-red-600">
              Authentication Error
            </h1>
            <p className="mt-2 text-gray-600">{error}</p>
            <button
              onClick={() => navigate({ to: '/login' })}
              className="mt-4 rounded-md bg-blue-600 px-4 py-2 text-white hover:bg-blue-700"
            >
              Back to Login
            </button>
          </div>
        </div>
      </Container>
    )
  }

  return (
    <Container>
      <div className="flex min-h-screen flex-col items-center justify-center">
        <div className="text-center">
          <h1 className="text-2xl font-bold text-gray-900">
            Authenticating...
          </h1>
          <p className="mt-2 text-gray-600">
            Please wait while we verify your credentials.
          </p>
        </div>
      </div>
    </Container>
  )
}
