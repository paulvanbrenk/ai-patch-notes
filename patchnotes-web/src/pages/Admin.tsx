import { useState, useEffect } from 'react'
import { Link, useNavigate } from '@tanstack/react-router'
import { useStytchUser } from '@stytch/react'
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
  useDeletePackage,
  useUpdatePackage,
  useBulkAddPackages,
} from '../api/hooks'
import type { PackageDto } from '../api/generated/model'

function formatDate(dateString: string | null | undefined): string {
  if (!dateString) return 'Never'
  return new Date(dateString).toLocaleString()
}

// ── Auth Gate ─────────────────────────────────────────────────

function useIsAdmin(): { isAdmin: boolean; isLoading: boolean } {
  const { user, isInitialized } = useStytchUser()
  if (!isInitialized) return { isAdmin: false, isLoading: true }
  if (!user) return { isAdmin: false, isLoading: false }
  const roles = (user as { roles?: string[] }).roles ?? []
  return { isAdmin: roles.includes('patch_notes_admin'), isLoading: false }
}

// ── Edit Modal ────────────────────────────────────────────────

interface EditPackageModalProps {
  open: boolean
  onClose: () => void
  pkg: PackageDto | null
}

function EditPackageModal({ open, onClose, pkg }: EditPackageModalProps) {
  const [githubOwner, setGithubOwner] = useState(pkg?.githubOwner ?? '')
  const [githubRepo, setGithubRepo] = useState(pkg?.githubRepo ?? '')
  const [tagPrefix, setTagPrefix] = useState(pkg?.tagPrefix ?? '')
  const updatePackage = useUpdatePackage()

  const handleSave = async () => {
    if (!pkg) return

    await updatePackage.mutateAsync({
      id: pkg.id,
      githubOwner: githubOwner.trim() || undefined,
      githubRepo: githubRepo.trim() || undefined,
      tagPrefix: tagPrefix.trim(),
    })
    onClose()
  }

  if (!open) return null

  return (
    <Modal
      open={open}
      onClose={onClose}
      title={`Edit ${pkg?.name ?? pkg?.npmName}`}
    >
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
        <Input
          label="Tag Prefix"
          value={tagPrefix}
          onChange={(e) => setTagPrefix(e.target.value)}
          placeholder="e.g., v (leave blank for none)"
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

// ── Delete Confirmation Modal ─────────────────────────────────

interface DeleteConfirmModalProps {
  open: boolean
  onClose: () => void
  onConfirm: () => void
  pkg: PackageDto | null
  isPending: boolean
}

function DeleteConfirmModal({
  open,
  onClose,
  onConfirm,
  pkg,
  isPending,
}: DeleteConfirmModalProps) {
  if (!open) return null

  return (
    <Modal open={open} onClose={onClose} title="Delete Package">
      <div className="space-y-4">
        <p className="text-text-secondary">
          Are you sure you want to delete{' '}
          <span className="font-medium text-text-primary">
            {pkg?.name ??
              pkg?.npmName ??
              `${pkg?.githubOwner}/${pkg?.githubRepo}`}
          </span>
          ? This action cannot be undone.
        </p>
        <div className="flex justify-end gap-2 pt-2">
          <Button variant="secondary" onClick={onClose} disabled={isPending}>
            Cancel
          </Button>
          <Button
            onClick={onConfirm}
            disabled={isPending}
            className="bg-major hover:bg-major/90 text-white"
          >
            {isPending ? 'Deleting...' : 'Delete'}
          </Button>
        </div>
      </div>
    </Modal>
  )
}

// ── Add Package Form ──────────────────────────────────────────

interface AddPackageFormProps {
  onClose: () => void
}

function AddPackageForm({ onClose }: AddPackageFormProps) {
  const [githubOwner, setGithubOwner] = useState('')
  const [githubRepo, setGithubRepo] = useState('')
  const [name, setName] = useState('')
  const [npmName, setNpmName] = useState('')
  const [tagPrefix, setTagPrefix] = useState('')
  const bulkAdd = useBulkAddPackages()

  const canSubmit = githubOwner.trim() && githubRepo.trim()

  const handleSubmit = async () => {
    if (!canSubmit) return

    await bulkAdd.mutateAsync([
      {
        githubOwner: githubOwner.trim(),
        githubRepo: githubRepo.trim(),
        name: name.trim() || undefined,
        npmName: npmName.trim() || undefined,
        tagPrefix: tagPrefix.trim() || undefined,
      },
    ])
    onClose()
  }

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter' && !e.shiftKey && canSubmit) {
      e.preventDefault()
      handleSubmit()
    } else if (e.key === 'Escape') {
      onClose()
    }
  }

  return (
    <Card className="mb-6">
      <h3 className="text-sm font-semibold text-text-primary mb-3">
        Add New Package
      </h3>
      <div className="grid grid-cols-1 sm:grid-cols-2 gap-3 max-w-2xl">
        <Input
          label="GitHub Owner *"
          placeholder="e.g., facebook"
          value={githubOwner}
          onChange={(e) => setGithubOwner(e.target.value)}
          onKeyDown={handleKeyDown}
          disabled={bulkAdd.isPending}
          autoFocus
        />
        <Input
          label="GitHub Repo *"
          placeholder="e.g., react"
          value={githubRepo}
          onChange={(e) => setGithubRepo(e.target.value)}
          onKeyDown={handleKeyDown}
          disabled={bulkAdd.isPending}
        />
        <Input
          label="Display Name"
          placeholder="Optional"
          value={name}
          onChange={(e) => setName(e.target.value)}
          onKeyDown={handleKeyDown}
          disabled={bulkAdd.isPending}
        />
        <Input
          label="npm Name"
          placeholder="Optional"
          value={npmName}
          onChange={(e) => setNpmName(e.target.value)}
          onKeyDown={handleKeyDown}
          disabled={bulkAdd.isPending}
        />
        <Input
          label="Tag Prefix"
          placeholder="e.g., v"
          value={tagPrefix}
          onChange={(e) => setTagPrefix(e.target.value)}
          onKeyDown={handleKeyDown}
          disabled={bulkAdd.isPending}
        />
      </div>
      <div className="flex gap-2 mt-4">
        <Button
          size="sm"
          onClick={handleSubmit}
          disabled={!canSubmit || bulkAdd.isPending}
        >
          {bulkAdd.isPending ? 'Adding...' : 'Add'}
        </Button>
        <Button
          variant="secondary"
          size="sm"
          onClick={onClose}
          disabled={bulkAdd.isPending}
        >
          Cancel
        </Button>
      </div>
      {bulkAdd.error && (
        <p className="mt-2 text-sm text-major">
          Failed to add package. Please try again.
        </p>
      )}
    </Card>
  )
}

// ── Bulk Add Form ─────────────────────────────────────────────

interface BulkAddFormProps {
  onClose: () => void
}

function BulkAddForm({ onClose }: BulkAddFormProps) {
  const [text, setText] = useState('')
  const bulkAdd = useBulkAddPackages()
  const [results, setResults] = useState<
    | {
        githubOwner?: string | null
        githubRepo?: string | null
        success?: boolean
        error?: string | null
      }[]
    | null
  >(null)

  const lines = text
    .split('\n')
    .map((l) => l.trim())
    .filter(Boolean)

  const handleSubmit = async () => {
    if (lines.length === 0) return

    const items = lines.map((line) => {
      const parts = line.split('/')
      return {
        githubOwner: parts[0]?.trim() ?? '',
        githubRepo: parts[1]?.trim() ?? '',
      }
    })

    const res = await bulkAdd.mutateAsync(items)
    const data = res as { data?: { results?: typeof results } }
    if (data?.data?.results) {
      setResults(data.data.results)
    } else {
      onClose()
    }
  }

  return (
    <Card className="mb-6">
      <h3 className="text-sm font-semibold text-text-primary mb-1">
        Bulk Add Packages
      </h3>
      <p className="text-xs text-text-secondary mb-3">
        Paste one owner/repo per line (e.g., facebook/react)
      </p>
      <textarea
        className="w-full max-w-lg px-3 py-2 bg-surface-primary border border-border-default rounded-lg text-text-primary placeholder:text-text-tertiary text-sm font-mono focus:outline-none focus:ring-2 focus:ring-brand-500 focus:border-transparent disabled:opacity-50"
        rows={6}
        placeholder={`facebook/react\nvercel/next.js\nvuejs/core`}
        value={text}
        onChange={(e) => {
          setText(e.target.value)
          setResults(null)
        }}
        disabled={bulkAdd.isPending}
        autoFocus
      />
      <div className="flex gap-2 mt-3">
        <Button
          size="sm"
          onClick={handleSubmit}
          disabled={lines.length === 0 || bulkAdd.isPending}
        >
          {bulkAdd.isPending
            ? 'Adding...'
            : `Add ${lines.length} package${lines.length !== 1 ? 's' : ''}`}
        </Button>
        <Button
          variant="secondary"
          size="sm"
          onClick={onClose}
          disabled={bulkAdd.isPending}
        >
          Cancel
        </Button>
      </div>
      {bulkAdd.error && (
        <p className="mt-2 text-sm text-major">
          Failed to bulk add packages. Please try again.
        </p>
      )}
      {results && (
        <div className="mt-3 space-y-1">
          {results.map((r, i) => (
            <div
              key={i}
              className={`text-xs ${r.success ? 'text-minor' : 'text-major'}`}
            >
              {r.githubOwner}/{r.githubRepo}:{' '}
              {r.success ? 'Added' : (r.error ?? 'Failed')}
            </div>
          ))}
        </div>
      )}
    </Card>
  )
}

// ── Package Row ───────────────────────────────────────────────

interface PackageRowProps {
  pkg: PackageDto
  onEdit: (pkg: PackageDto) => void
  onDelete: (pkg: PackageDto) => void
  isDeleting: boolean
}

function PackageRow({ pkg, onEdit, onDelete, isDeleting }: PackageRowProps) {
  const githubUrl = `https://github.com/${pkg.githubOwner}/${pkg.githubRepo}`

  return (
    <tr className="border-b border-border-default last:border-0">
      <td className="py-3 px-4">
        <span className="font-medium text-text-primary">{pkg.name}</span>
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
        {pkg.npmName ?? '\u2014'}
      </td>
      <td className="py-3 px-4 text-text-secondary text-sm font-mono">
        {pkg.tagPrefix ?? '\u2014'}
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

// ── Main Admin Page ───────────────────────────────────────────

export function Admin() {
  const { isAdmin, isLoading: authLoading } = useIsAdmin()
  const navigate = useNavigate()
  const { data: packages, isLoading } = usePackages()
  const deletePackage = useDeletePackage()

  const [showAddForm, setShowAddForm] = useState(false)
  const [showBulkAdd, setShowBulkAdd] = useState(false)
  const [editingPackage, setEditingPackage] = useState<PackageDto | null>(null)
  const [deletingPackage, setDeletingPackage] = useState<PackageDto | null>(
    null
  )
  const [deletingId, setDeletingId] = useState<string | null>(null)

  // Auth gate: redirect non-admins
  useEffect(() => {
    if (!authLoading && !isAdmin) {
      navigate({ to: '/' })
    }
  }, [authLoading, isAdmin, navigate])

  if (authLoading || !isAdmin) {
    return (
      <div className="min-h-screen bg-surface-secondary flex items-center justify-center">
        <p className="text-text-secondary">
          {authLoading ? 'Checking access...' : 'Redirecting...'}
        </p>
      </div>
    )
  }

  const handleDeleteConfirm = async () => {
    if (!deletingPackage) return
    setDeletingId(deletingPackage.id)
    await deletePackage
      .mutateAsync(deletingPackage.id)
      .finally(() => setDeletingId(null))
    setDeletingPackage(null)
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
          <Button
            variant="secondary"
            size="sm"
            onClick={() => {
              setShowBulkAdd(true)
              setShowAddForm(false)
            }}
          >
            Bulk Add
          </Button>
          <Button
            size="sm"
            onClick={() => {
              setShowAddForm(true)
              setShowBulkAdd(false)
            }}
          >
            Add Package
          </Button>
        </div>
      </Header>

      <main className="py-8">
        <Container>
          {/* Add Package Form */}
          {showAddForm && (
            <AddPackageForm onClose={() => setShowAddForm(false)} />
          )}

          {/* Bulk Add Form */}
          {showBulkAdd && <BulkAddForm onClose={() => setShowBulkAdd(false)} />}

          {/* Packages Table */}
          <Card padding="none">
            <div className="px-6 py-4 border-b border-border-default">
              <h2 className="text-lg font-semibold text-text-primary">
                Tracked Packages
              </h2>
              <p className="text-sm text-text-secondary mt-1">
                {packages
                  ? `${packages.length} package${packages.length !== 1 ? 's' : ''} tracked`
                  : 'Loading...'}
              </p>
            </div>

            {isLoading ? (
              <div className="p-6 text-text-secondary">Loading packages...</div>
            ) : packages?.length === 0 ? (
              <div className="p-6 text-text-secondary">
                No packages tracked yet. Click &ldquo;Add Package&rdquo; to get
                started.
              </div>
            ) : (
              <div className="overflow-x-auto">
                <table className="w-full">
                  <thead>
                    <tr className="border-b border-border-default bg-surface-secondary">
                      <th className="py-3 px-4 text-left text-sm font-medium text-text-secondary">
                        Name
                      </th>
                      <th className="py-3 px-4 text-left text-sm font-medium text-text-secondary">
                        GitHub Repo
                      </th>
                      <th className="py-3 px-4 text-left text-sm font-medium text-text-secondary">
                        npm Name
                      </th>
                      <th className="py-3 px-4 text-left text-sm font-medium text-text-secondary">
                        Tag Prefix
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
                        onDelete={setDeletingPackage}
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

      <DeleteConfirmModal
        open={!!deletingPackage}
        onClose={() => setDeletingPackage(null)}
        onConfirm={handleDeleteConfirm}
        pkg={deletingPackage}
        isPending={deletingId === deletingPackage?.id}
      />
    </div>
  )
}
