import { useState } from 'react'
import { Link } from '@tanstack/react-router'
import {
  Header,
  HeaderTitle,
  Container,
  Button,
  Input,
  Modal,
  Card,
} from '../components/ui'
import {
  usePackages,
  useAddPackage,
  useDeletePackage,
  useUpdatePackage,
} from '../api/hooks'
import type { Package } from '../api/types'

function formatDate(dateString: string | null): string {
  if (!dateString) return 'Never'
  return new Date(dateString).toLocaleString()
}

interface EditPackageModalProps {
  open: boolean
  onClose: () => void
  pkg: Package | null
}

function EditPackageModal({ open, onClose, pkg }: EditPackageModalProps) {
  const [githubOwner, setGithubOwner] = useState(pkg?.githubOwner ?? '')
  const [githubRepo, setGithubRepo] = useState(pkg?.githubRepo ?? '')
  const updatePackage = useUpdatePackage()

  const handleSave = async () => {
    if (!pkg) return

    await updatePackage.mutateAsync({
      id: pkg.id,
      githubOwner: githubOwner.trim() || undefined,
      githubRepo: githubRepo.trim() || undefined,
    })
    onClose()
  }

  if (!open) return null

  return (
    <Modal open={open} onClose={onClose} title={`Edit ${pkg?.npmName}`}>
      <div className="space-y-4">
        <Input
          label="GitHub Owner"
          value={githubOwner}
          onChange={(e) => setGithubOwner(e.target.value)}
          placeholder="e.g., facebook"
        />
        <Input
          label="GitHub Repo"
          value={githubRepo}
          onChange={(e) => setGithubRepo(e.target.value)}
          placeholder="e.g., react"
        />
        <div className="flex justify-end gap-2 pt-2">
          <Button variant="secondary" onClick={onClose}>
            Cancel
          </Button>
          <Button onClick={handleSave} disabled={updatePackage.isPending}>
            {updatePackage.isPending ? 'Saving...' : 'Save'}
          </Button>
        </div>
      </div>
    </Modal>
  )
}

interface PackageRowProps {
  pkg: Package
  onEdit: (pkg: Package) => void
  onDelete: (pkg: Package) => void
  isDeleting: boolean
}

function PackageRow({ pkg, onEdit, onDelete, isDeleting }: PackageRowProps) {
  const githubUrl = `https://github.com/${pkg.githubOwner}/${pkg.githubRepo}`

  return (
    <tr className="border-b border-border-default last:border-0">
      <td className="py-3 px-4">
        <span className="font-medium text-text-primary">{pkg.npmName}</span>
      </td>
      <td className="py-3 px-4">
        <a
          href={githubUrl}
          target="_blank"
          rel="noopener noreferrer"
          className="text-brand-600 hover:text-brand-700 hover:underline"
        >
          {pkg.githubOwner}/{pkg.githubRepo}
        </a>
      </td>
      <td className="py-3 px-4 text-text-secondary text-sm">
        {formatDate(pkg.lastFetchedAt)}
      </td>
      <td className="py-3 px-4">
        <div className="flex gap-2 justify-end">
          <Button variant="secondary" size="sm" onClick={() => onEdit(pkg)}>
            Edit
          </Button>
          <Button
            variant="ghost"
            size="sm"
            onClick={() => onDelete(pkg)}
            disabled={isDeleting}
            className="text-major hover:text-major"
          >
            {isDeleting ? 'Deleting...' : 'Delete'}
          </Button>
        </div>
      </td>
    </tr>
  )
}

export function Admin() {
  const { data: packages, isLoading } = usePackages()
  const addPackage = useAddPackage()
  const deletePackage = useDeletePackage()

  const [showAddForm, setShowAddForm] = useState(false)
  const [newPackageName, setNewPackageName] = useState('')
  const [editingPackage, setEditingPackage] = useState<Package | null>(null)
  const [deletingId, setDeletingId] = useState<string | null>(null)

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

  const handleDeletePackage = async (pkg: Package) => {
    if (!confirm(`Are you sure you want to delete ${pkg.npmName}?`)) return

    setDeletingId(pkg.id)
    try {
      await deletePackage.mutateAsync(pkg.id)
    } finally {
      setDeletingId(null)
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
        <HeaderTitle>Package Management</HeaderTitle>
        <div className="flex items-center gap-3">
          <Link to="/">
            <Button variant="secondary" size="sm">
              Back to Home
            </Button>
          </Link>
          <Button size="sm" onClick={() => setShowAddForm(true)}>
            Add Package
          </Button>
        </div>
      </Header>

      <main className="py-8">
        <Container>
          {/* Add Package Form */}
          {showAddForm && (
            <Card className="mb-8">
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
            </Card>
          )}

          {/* Packages Table */}
          <Card padding="none">
            <div className="px-6 py-4 border-b border-border-default">
              <h2 className="text-lg font-semibold text-text-primary">
                Tracked Packages
              </h2>
              <p className="text-sm text-text-secondary mt-1">
                Manage the npm packages you're tracking for release notes.
              </p>
            </div>

            {isLoading ? (
              <div className="p-6 text-text-secondary">Loading packages...</div>
            ) : packages?.length === 0 ? (
              <div className="p-6 text-text-secondary">
                No packages tracked yet. Click "Add Package" to get started.
              </div>
            ) : (
              <div className="overflow-x-auto">
                <table className="w-full">
                  <thead>
                    <tr className="border-b border-border-default bg-surface-secondary">
                      <th className="py-3 px-4 text-left text-sm font-medium text-text-secondary">
                        Package
                      </th>
                      <th className="py-3 px-4 text-left text-sm font-medium text-text-secondary">
                        GitHub Repository
                      </th>
                      <th className="py-3 px-4 text-left text-sm font-medium text-text-secondary">
                        Last Synced
                      </th>
                      <th className="py-3 px-4 text-right text-sm font-medium text-text-secondary">
                        Actions
                      </th>
                    </tr>
                  </thead>
                  <tbody>
                    {packages?.map((pkg) => (
                      <PackageRow
                        key={pkg.id}
                        pkg={pkg}
                        onEdit={setEditingPackage}
                        onDelete={handleDeletePackage}
                        isDeleting={deletingId === pkg.id}
                      />
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </Card>
        </Container>
      </main>

      <EditPackageModal
        key={editingPackage?.id}
        open={!!editingPackage}
        onClose={() => setEditingPackage(null)}
        pkg={editingPackage}
      />
    </div>
  )
}
