import { useState, useEffect, useCallback } from 'react'
import { Card } from '../ui/Card'
import { Button } from '../ui/Button'
import { Input } from '../ui/Input'
import { Checkbox } from '../ui/Checkbox'

interface Package {
  id: number
  npmName: string
  githubOwner: string
  githubRepo: string
}

interface PackagePickerProps {
  packages: Package[]
  isLoading?: boolean
  onSelectionChange?: (selectedIds: number[]) => void
  onAddPackage?: (npmName: string) => void
  storageKey?: string
}

const STORAGE_KEY_PREFIX = 'patchnotes:package-selection:'

function PackageItemSkeleton() {
  return (
    <div className="flex items-center gap-3 py-3 px-4 animate-pulse">
      <div className="w-5 h-5 rounded-md bg-surface-tertiary" />
      <div className="flex-1 space-y-2">
        <div className="h-4 w-24 rounded bg-surface-tertiary" />
        <div className="h-3 w-32 rounded bg-surface-tertiary" />
      </div>
    </div>
  )
}

export function PackagePicker({
  packages,
  isLoading = false,
  onSelectionChange,
  onAddPackage,
  storageKey = 'default',
}: PackagePickerProps) {
  const fullStorageKey = `${STORAGE_KEY_PREFIX}${storageKey}`

  const [selectedIds, setSelectedIds] = useState<Set<number>>(() => {
    if (typeof window === 'undefined') return new Set()
    try {
      const stored = localStorage.getItem(fullStorageKey)
      if (stored) {
        const parsed = JSON.parse(stored)
        return new Set(Array.isArray(parsed) ? parsed : [])
      }
    } catch {
      // Ignore storage errors
    }
    return new Set()
  })

  const [newPackageName, setNewPackageName] = useState('')
  const [isAdding, setIsAdding] = useState(false)

  // Persist selection to localStorage
  useEffect(() => {
    try {
      localStorage.setItem(fullStorageKey, JSON.stringify([...selectedIds]))
    } catch {
      // Ignore storage errors
    }
  }, [selectedIds, fullStorageKey])

  // Notify parent of selection changes
  useEffect(() => {
    onSelectionChange?.([...selectedIds])
  }, [selectedIds, onSelectionChange])

  const handleToggle = useCallback((id: number) => {
    setSelectedIds((prev) => {
      const next = new Set(prev)
      if (next.has(id)) {
        next.delete(id)
      } else {
        next.add(id)
      }
      return next
    })
  }, [])

  const handleSelectAll = useCallback(() => {
    setSelectedIds(new Set(packages.map((p) => p.id)))
  }, [packages])

  const handleDeselectAll = useCallback(() => {
    setSelectedIds(new Set())
  }, [])

  const handleAddPackage = useCallback(async () => {
    if (!newPackageName.trim() || !onAddPackage) return

    setIsAdding(true)
    try {
      await onAddPackage(newPackageName.trim())
      setNewPackageName('')
    } finally {
      setIsAdding(false)
    }
  }, [newPackageName, onAddPackage])

  const handleKeyDown = useCallback(
    (e: React.KeyboardEvent) => {
      if (e.key === 'Enter' && !e.shiftKey) {
        e.preventDefault()
        handleAddPackage()
      }
    },
    [handleAddPackage]
  )

  const allSelected =
    packages.length > 0 && selectedIds.size === packages.length
  const someSelected =
    selectedIds.size > 0 && selectedIds.size < packages.length
  const selectedCount = selectedIds.size

  return (
    <Card padding="none" className="overflow-hidden">
      {/* Header */}
      <div className="px-4 py-3 border-b border-border-default bg-surface-secondary/50">
        <div className="flex items-center justify-between gap-4">
          <div className="flex items-center gap-3">
            <h3 className="text-sm font-semibold text-text-primary">
              Packages
            </h3>
            {!isLoading && packages.length > 0 && (
              <span className="text-xs text-text-tertiary tabular-nums">
                {selectedCount} of {packages.length} selected
              </span>
            )}
          </div>
          {!isLoading && packages.length > 0 && (
            <div className="flex items-center gap-2">
              <Button
                variant="ghost"
                size="sm"
                onClick={allSelected ? handleDeselectAll : handleSelectAll}
                className="text-xs h-7 px-2"
              >
                {allSelected
                  ? 'Deselect all'
                  : someSelected
                    ? 'Select all'
                    : 'Select all'}
              </Button>
            </div>
          )}
        </div>
      </div>

      {/* Package List */}
      <div className="divide-y divide-border-muted max-h-80 overflow-y-auto">
        {isLoading ? (
          <>
            <PackageItemSkeleton />
            <PackageItemSkeleton />
            <PackageItemSkeleton />
          </>
        ) : packages.length === 0 ? (
          <div className="py-8 px-4 text-center">
            <p className="text-sm text-text-tertiary">
              No packages tracked yet
            </p>
          </div>
        ) : (
          packages.map((pkg) => (
            <div
              key={pkg.id}
              className={`
                py-3 px-4 transition-colors duration-150
                hover:bg-surface-secondary/50
                ${selectedIds.has(pkg.id) ? 'bg-brand-50/50 dark:bg-brand-900/10' : ''}
              `}
            >
              <Checkbox
                checked={selectedIds.has(pkg.id)}
                onChange={() => handleToggle(pkg.id)}
                label={pkg.npmName}
                description={`${pkg.githubOwner}/${pkg.githubRepo}`}
              />
            </div>
          ))
        )}
      </div>

      {/* Add Package Input */}
      {onAddPackage && (
        <div className="px-4 py-3 border-t border-border-default bg-surface-secondary/30">
          <div className="flex gap-2">
            <div className="flex-1">
              <Input
                placeholder="Add package (e.g., lodash)"
                value={newPackageName}
                onChange={(e) => setNewPackageName(e.target.value)}
                onKeyDown={handleKeyDown}
                disabled={isAdding}
                className="text-sm"
              />
            </div>
            <Button
              size="sm"
              onClick={handleAddPackage}
              disabled={!newPackageName.trim() || isAdding}
              className="flex-shrink-0"
            >
              {isAdding ? (
                <span className="inline-flex items-center gap-1.5">
                  <svg
                    className="w-4 h-4 animate-spin"
                    viewBox="0 0 24 24"
                    fill="none"
                  >
                    <circle
                      className="opacity-25"
                      cx="12"
                      cy="12"
                      r="10"
                      stroke="currentColor"
                      strokeWidth="4"
                    />
                    <path
                      className="opacity-75"
                      fill="currentColor"
                      d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"
                    />
                  </svg>
                  Adding...
                </span>
              ) : (
                'Add'
              )}
            </Button>
          </div>
        </div>
      )}
    </Card>
  )
}
