import { useState } from 'react'
import { Link, useNavigate } from '@tanstack/react-router'
import { useStytchUser } from '@stytch/react'
import { Header, HeaderTitle, Container, Button, Input } from '../components/ui'
import { PackageCard, ReleaseCard } from '../components/releases'
import { usePackages, useReleases, useAddPackage } from '../api/hooks'
import type { Release } from '../api/types'
import { SettingsModal } from '../components/settings'
import { UserMenu } from '../components/auth'

function getReleaseUrl(release: Release): string {
  const { githubOwner, githubRepo } = release.package
  return `https://github.com/${githubOwner}/${githubRepo}/releases/tag/${release.tag}`
}

export function Home() {
  const navigate = useNavigate()
  const { user } = useStytchUser()
  const { data: packages, isLoading: packagesLoading } = usePackages()
  const { data: releases, isLoading: releasesLoading } = useReleases()
  const addPackage = useAddPackage()
  const [showAddForm, setShowAddForm] = useState(false)
  const [newPackageName, setNewPackageName] = useState('')
  const [settingsOpen, setSettingsOpen] = useState(false)

  const isLoggedIn = !!user
  const isAdmin =
    user?.roles?.some((r) => r.role_id === 'patch_notes_admin') ?? false

  const handleAddPackage = async () => {
    if (!newPackageName.trim()) return

    try {
      await addPackage.mutateAsync(newPackageName.trim())
      setNewPackageName('')
      setShowAddForm(false)
    } catch (error) {
      console.error('Failed to add package:', error)
    }
  }

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault()
      handleAddPackage()
    } else if (e.key === 'Escape') {
      setShowAddForm(false)
      setNewPackageName('')
    }
  }

  return (
    <div className="min-h-screen bg-surface-secondary">
      <Header>
        <HeaderTitle>Patch Notes</HeaderTitle>
        <div className="flex items-center gap-3">
          <Link to="/preview">
            <Button variant="secondary" size="sm">
              Preview
            </Button>
          </Link>
          {isAdmin && (
            <Link to="/admin">
              <Button variant="secondary" size="sm">
                Admin
              </Button>
            </Link>
          )}
          <Button
            variant="secondary"
            size="sm"
            onClick={() => setSettingsOpen(true)}
          >
            Settings
          </Button>
          {isLoggedIn && (
            <Button size="sm" onClick={() => setShowAddForm(true)}>
              Add Package
            </Button>
          )}
          <div className="ml-2 border-l border-border-default pl-4">
            <UserMenu />
          </div>
        </div>
      </Header>

      <main className="py-8">
        <Container>
          {/* Add Package Form */}
          {isLoggedIn && showAddForm && (
            <div className="mb-8 p-4 bg-surface-primary rounded-lg border border-border-default">
              <h3 className="text-sm font-semibold text-text-primary mb-3">
                Add New Package
              </h3>
              <div className="flex gap-2 max-w-md">
                <Input
                  placeholder="Package name (e.g., lodash)"
                  value={newPackageName}
                  onChange={(e) => setNewPackageName(e.target.value)}
                  onKeyDown={handleKeyDown}
                  disabled={addPackage.isPending}
                  autoFocus
                />
                <Button
                  size="sm"
                  onClick={handleAddPackage}
                  disabled={!newPackageName.trim() || addPackage.isPending}
                >
                  {addPackage.isPending ? 'Adding...' : 'Add'}
                </Button>
                <Button
                  variant="secondary"
                  size="sm"
                  onClick={() => {
                    setShowAddForm(false)
                    setNewPackageName('')
                  }}
                  disabled={addPackage.isPending}
                >
                  Cancel
                </Button>
              </div>
              {addPackage.error && (
                <p className="mt-2 text-sm text-major">
                  Failed to add package. Please check the name and try again.
                </p>
              )}
            </div>
          )}

          {/* Search */}
          <div className="mb-8">
            <Input
              placeholder="Search packages or releases..."
              className="max-w-md"
            />
          </div>

          {/* Packages Section */}
          <section className="mb-12">
            <h2 className="text-xl font-semibold text-text-primary mb-4">
              Tracked Packages
            </h2>
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
              {packagesLoading ? (
                <p className="text-text-secondary">Loading packages...</p>
              ) : packages?.length === 0 ? (
                <p className="text-text-secondary">
                  No packages tracked yet. Add a package to get started.
                </p>
              ) : (
                packages?.map((pkg) => (
                  <PackageCard
                    key={pkg.id}
                    npmName={pkg.npmName}
                    githubOwner={pkg.githubOwner}
                    githubRepo={pkg.githubRepo}
                    lastFetchedAt={pkg.lastFetchedAt}
                    onClick={() =>
                      navigate({
                        to: '/packages/$packageId',
                        params: { packageId: String(pkg.id) },
                      })
                    }
                  />
                ))
              )}
            </div>
          </section>

          {/* Recent Releases Section */}
          <section>
            <h2 className="text-xl font-semibold text-text-primary mb-4">
              Recent Releases
            </h2>
            <div className="space-y-4">
              {releasesLoading ? (
                <p className="text-text-secondary">Loading releases...</p>
              ) : releases?.length === 0 ? (
                <p className="text-text-secondary">
                  No releases found. Releases will appear here after syncing.
                </p>
              ) : (
                releases?.map((release) => (
                  <ReleaseCard
                    key={release.id}
                    tag={release.tag}
                    title={release.title}
                    body={release.body}
                    publishedAt={release.publishedAt}
                    htmlUrl={getReleaseUrl(release)}
                    onClick={() =>
                      navigate({
                        to: '/releases/$releaseId',
                        params: { releaseId: String(release.id) },
                      })
                    }
                  />
                ))
              )}
            </div>
          </section>
        </Container>
      </main>

      <SettingsModal
        open={settingsOpen}
        onClose={() => setSettingsOpen(false)}
      />
    </div>
  )
}
