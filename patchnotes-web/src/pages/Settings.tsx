import { useState, useEffect } from 'react'
import { Link, useNavigate } from '@tanstack/react-router'
import { useStytchUser } from '@stytch/react'
import { Header, HeaderTitle, Container, Button, Input } from '../components/ui'
import { ThemeToggle } from '../components/theme'
import { UserMenu } from '../components/auth'
import { Logo } from '../components/landing/Logo'
import {
  useGetCurrentUser,
  useUpdateCurrentUser,
} from '../api/generated/users/users'
import { getGetCurrentUserQueryKey } from '../api/generated/users/users'
import { useQueryClient } from '@tanstack/react-query'

export function Settings() {
  const { user, isInitialized } = useStytchUser()
  const navigate = useNavigate()
  const queryClient = useQueryClient()

  const { data: currentUser } = useGetCurrentUser({
    query: { enabled: !!user },
  })
  const updateUser = useUpdateCurrentUser()

  const [editedName, setEditedName] = useState<string | null>(null)
  const [saved, setSaved] = useState(false)

  const serverName =
    currentUser?.status === 200 ? (currentUser.data.name ?? '') : ''
  const name = editedName ?? serverName

  // Redirect if not authenticated
  useEffect(() => {
    if (isInitialized && !user) {
      navigate({ to: '/login' })
    }
  }, [isInitialized, user, navigate])

  const handleSave = () => {
    setSaved(false)
    updateUser.mutate(
      { data: { name: name || undefined } },
      {
        onSuccess: () => {
          setSaved(true)
          setEditedName(null)
          queryClient.invalidateQueries({
            queryKey: getGetCurrentUserQueryKey(),
          })
        },
      }
    )
  }

  if (!isInitialized || !user) {
    return null
  }

  return (
    <div className="min-h-screen bg-surface-secondary">
      <Header>
        <Link
          to="/"
          className="flex items-center gap-2.5 hover:opacity-80 transition-opacity"
        >
          <Logo size={36} />
          <div>
            <HeaderTitle>My Release Notes - Settings</HeaderTitle>
            <p className="text-2xs text-text-tertiary leading-tight">
              by Tiny Tools
            </p>
          </div>
        </Link>
        <div className="flex items-center gap-3">
          <ThemeToggle />
          <UserMenu />
        </div>
      </Header>

      <main className="py-12">
        <Container>
          <div className="max-w-lg mx-auto">
            <h1 className="text-2xl font-bold text-text-primary mb-8">
              Settings
            </h1>

            <div className="space-y-6">
              <Input
                label="Display Name"
                type="text"
                value={name}
                onChange={(e) => {
                  setEditedName(e.target.value)
                  setSaved(false)
                }}
                placeholder="Your name"
              />

              <div className="flex items-center gap-3">
                <Button onClick={handleSave} disabled={updateUser.isPending}>
                  {updateUser.isPending ? 'Saving...' : 'Save'}
                </Button>
                {saved && (
                  <span className="text-sm text-emerald-600 dark:text-emerald-400">
                    Saved successfully
                  </span>
                )}
                {updateUser.isError && (
                  <span className="text-sm text-red-600 dark:text-red-400">
                    Failed to save
                  </span>
                )}
              </div>
            </div>
          </div>
        </Container>
      </main>
    </div>
  )
}
