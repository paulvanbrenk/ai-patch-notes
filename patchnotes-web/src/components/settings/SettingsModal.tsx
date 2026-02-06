import { Modal, Checkbox } from '../ui'

interface SettingsModalProps {
  open: boolean
  onClose: () => void
}

export function SettingsModal({ open, onClose }: SettingsModalProps) {
  return (
    <Modal open={open} onClose={onClose} title="Settings">
      <div className="space-y-6">
        <section>
          <h3 className="text-sm font-medium text-text-primary mb-3">
            Notifications
          </h3>
          <div className="space-y-3">
            <Checkbox
              label="Email notifications"
              description="Receive email alerts for new releases"
            />
            <Checkbox
              label="Browser notifications"
              description="Show desktop notifications"
            />
          </div>
        </section>

        <section>
          <h3 className="text-sm font-medium text-text-primary mb-3">
            Display
          </h3>
          <div className="space-y-3">
            <Checkbox
              label="Show pre-releases"
              description="Include alpha, beta, and RC versions"
            />
            <Checkbox
              label="Compact view"
              description="Use smaller cards for releases"
            />
          </div>
        </section>
      </div>
    </Modal>
  )
}
