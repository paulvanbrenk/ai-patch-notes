import { useState, useEffect } from 'react'
import { Link, useNavigate } from '@tanstack/react-router'
import { useStytchUser } from '@stytch/react'
import { Header, HeaderTitle, Container, Button, Input } from '../components/ui'
import { ThemeToggle } from '../components/theme'
import { UserMenu } from '../components/auth'
import { Logo } from '../components/landing/Logo'
import { Checkbox } from '../components/ui/Checkbox'
import {
  useGetCurrentUser,
  useUpdateCurrentUser,
  useGetEmailPreferences,
  useUpdateEmailPreferences,
  getGetEmailPreferencesQueryKey,
} from '../api/generated/users/users'
import { getGetCurrentUserQueryKey } from '../api/generated/users/users'
import { useQueryClient } from '@tanstack/react-query'

const DAY_NAMES = [
  'Sunday',
  'Monday',
  'Tuesday',
  'Wednesday',
  'Thursday',
  'Friday',
  'Saturday',
]

function formatHour(hour: number): string {
  const ampm = hour < 12 ? 'AM' : 'PM'
  const h = hour % 12 === 0 ? 12 : hour % 12
  return `${h}:00 ${ampm}`
}

function utcHourToLocal(utcHour: number): number {
  const now = new Date()
  now.setUTCHours(utcHour, 0, 0, 0)
  return now.getHours()
}

function localHourToUtc(localHour: number): number {
  const now = new Date()
  now.setHours(localHour, 0, 0, 0)
  return now.getUTCHours()
}

function getLocalTimezoneAbbr(): string {
  return (
    new Intl.DateTimeFormat('en', { timeZoneName: 'short' })
      .formatToParts(new Date())
      .find((p) => p.type === 'timeZoneName')?.value ?? 'local'
  )
}

export function Settings() {
  const { user, isInitialized } = useStytchUser()
  const navigate = useNavigate()
  const queryClient = useQueryClient()

  const { data: currentUser, isSuccess } = useGetCurrentUser({
    query: { enabled: !!user },
  })
  const updateUser = useUpdateCurrentUser()

  const serverName =
    currentUser?.status === 200 ? (currentUser.data.name ?? '') : ''
  const [name, setName] = useState('')
  const [initialized, setInitialized] = useState(false)
  const [saved, setSaved] = useState(false)

  // Sync server data into form once on load
  if (isSuccess && !initialized) {
    setName(serverName)
    setInitialized(true)
  }

  // Redirect if not authenticated
  useEffect(() => {
    if (isInitialized && !user) {
      navigate({ to: '/login' })
    }
  }, [isInitialized, user, navigate])

  const handleSave = () => {
    setSaved(false)
    updateUser.mutate(
      { data: { name: name || null } },
      {
        onSuccess: () => {
          setSaved(true)
          setInitialized(false)
          queryClient.invalidateQueries({
            queryKey: getGetCurrentUserQueryKey(),
          })
        },
      }
    )
  }

  // Email preferences
  const isPro = currentUser?.status === 200 ? (currentUser.data.isPro ?? false) : false

  const { data: emailPrefs, isSuccess: emailPrefsLoaded } = useGetEmailPreferences({
    query: { enabled: !!user },
  })
  const updateEmailPrefs = useUpdateEmailPreferences()

  const serverDigestEnabled =
    emailPrefs?.status === 200 ? (emailPrefs.data.emailDigestEnabled ?? true) : true
  const serverDigestDay =
    emailPrefs?.status === 200 ? (emailPrefs.data.digestDay ?? 1) : 1
  const serverDigestHour =
    emailPrefs?.status === 200 ? (emailPrefs.data.digestHour ?? 4) : 4

  const [digestEnabled, setDigestEnabled] = useState(true)
  const [digestDay, setDigestDay] = useState(1)
  const [digestHour, setDigestHour] = useState(utcHourToLocal(4))
  const [emailPrefsInitialized, setEmailPrefsInitialized] = useState(false)
  const [emailPrefsSaved, setEmailPrefsSaved] = useState(false)

  if (emailPrefsLoaded && !emailPrefsInitialized) {
    setDigestEnabled(serverDigestEnabled)
    setDigestDay(serverDigestDay)
    setDigestHour(utcHourToLocal(serverDigestHour))
    setEmailPrefsInitialized(true)
  }

  const handleSaveEmailPrefs = () => {
    setEmailPrefsSaved(false)
    updateEmailPrefs.mutate(
      {
        data: {
          emailDigestEnabled: digestEnabled,
          digestDay,
          digestHour: localHourToUtc(digestHour),
        },
      },
      {
        onSuccess: () => {
          setEmailPrefsSaved(true)
          setEmailPrefsInitialized(false)
          queryClient.invalidateQueries({
            queryKey: getGetEmailPreferencesQueryKey(),
          })
        },
      }
    )
  }

  const tzAbbr = getLocalTimezoneAbbr()
  const scheduleText = `Your digest will be sent every ${DAY_NAMES[digestDay]} at ${formatHour(digestHour)} ${tzAbbr}`

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
                  setName(e.target.value)
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

            <div className="mt-10 pt-8 border-t border-border-default">
              <h2 className="text-lg font-semibold text-text-primary mb-4">
                Email Digest Schedule
              </h2>

              <div className={`space-y-4 ${!isPro ? 'opacity-60' : ''}`}>
                <Checkbox
                  label="Enable email digest"
                  description="Receive a weekly digest of release notes in your inbox"
                  checked={digestEnabled}
                  onChange={(e) => {
                    setDigestEnabled(e.target.checked)
                    setEmailPrefsSaved(false)
                  }}
                  disabled={!isPro}
                />

                <div className="grid grid-cols-2 gap-4">
                  <div>
                    <label className="block text-sm font-medium text-text-primary mb-1.5">
                      Day
                    </label>
                    <select
                      value={digestDay}
                      onChange={(e) => {
                        setDigestDay(Number(e.target.value))
                        setEmailPrefsSaved(false)
                      }}
                      disabled={!isPro}
                      className="w-full px-3 py-2 text-sm rounded-lg border border-border-default bg-surface-primary text-text-primary focus:outline-none focus:ring-2 focus:ring-brand-500 disabled:opacity-50 disabled:cursor-not-allowed"
                    >
                      {DAY_NAMES.map((day, i) => (
                        <option key={day} value={i}>
                          {day}
                        </option>
                      ))}
                    </select>
                  </div>

                  <div>
                    <label className="block text-sm font-medium text-text-primary mb-1.5">
                      Hour ({tzAbbr})
                    </label>
                    <select
                      value={digestHour}
                      onChange={(e) => {
                        setDigestHour(Number(e.target.value))
                        setEmailPrefsSaved(false)
                      }}
                      disabled={!isPro}
                      className="w-full px-3 py-2 text-sm rounded-lg border border-border-default bg-surface-primary text-text-primary focus:outline-none focus:ring-2 focus:ring-brand-500 disabled:opacity-50 disabled:cursor-not-allowed"
                    >
                      {Array.from({ length: 24 }, (_, i) => (
                        <option key={i} value={i}>
                          {formatHour(i)}
                        </option>
                      ))}
                    </select>
                  </div>
                </div>

                {isPro && (
                  <p className="text-sm text-text-secondary">{scheduleText}</p>
                )}
              </div>

              {!isPro && (
                <div className="mt-3 p-3 rounded-lg bg-surface-primary border border-border-default">
                  <p className="text-sm text-text-secondary">
                    <Link
                      to="/pricing"
                      className="font-medium text-brand-600 hover:text-brand-700 underline"
                    >
                      Upgrade to Pro
                    </Link>{' '}
                    to customize your digest schedule
                  </p>
                </div>
              )}

              {isPro && (
                <div className="mt-4 flex items-center gap-3">
                  <Button
                    onClick={handleSaveEmailPrefs}
                    disabled={updateEmailPrefs.isPending}
                  >
                    {updateEmailPrefs.isPending ? 'Saving...' : 'Save Schedule'}
                  </Button>
                  {emailPrefsSaved && (
                    <span className="text-sm text-emerald-600 dark:text-emerald-400">
                      Saved successfully
                    </span>
                  )}
                  {updateEmailPrefs.isError && (
                    <span className="text-sm text-red-600 dark:text-red-400">
                      Failed to save
                    </span>
                  )}
                </div>
              )}
            </div>
          </div>
        </Container>
      </main>
    </div>
  )
}
