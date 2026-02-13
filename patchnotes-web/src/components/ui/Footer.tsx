import { Hammer, Anvil } from 'lucide-react'
import { Container } from './Container'

export function Footer() {
  return (
    <footer className="mt-auto bg-surface-primary/80 backdrop-blur-md border-t border-border-default">
      <Container>
        <div className="py-6 flex flex-col gap-6">
          {/* Top: Brand + Nav */}
          <div className="flex flex-col sm:flex-row sm:items-start sm:justify-between gap-4">
            {/* Left: Brand */}
            <div>
              <p className="text-sm font-semibold text-text-primary">
                My Release Notes
              </p>
              <p className="text-xs text-text-tertiary">
                by{' '}
                <a
                  href="https://www.yourtinytools.com"
                  target="_blank"
                  rel="noopener noreferrer"
                  className="text-text-secondary hover:text-text-primary transition-colors"
                >
                  Tiny Tools
                </a>
              </p>
            </div>

            {/* Right: Nav links */}
            <nav className="flex flex-wrap gap-x-4 gap-y-1 text-sm text-text-secondary">
              <a href="/" className="hover:text-text-primary transition-colors">
                Home
              </a>
              <a
                href="/pricing"
                className="hover:text-text-primary transition-colors"
              >
                Pricing
              </a>
              <a
                href="/about"
                className="hover:text-text-primary transition-colors"
              >
                About
              </a>
              <a
                href="/privacy"
                className="hover:text-text-primary transition-colors"
              >
                Privacy
              </a>
            </nav>
          </div>

          {/* Bottom: Tagline + Copyright */}
          <div className="flex flex-col items-center gap-1.5 border-t border-border-muted pt-4">
            <div className="flex items-center gap-1.5">
              <Hammer
                className="w-4 h-4 text-text-tertiary"
                strokeWidth={1.5}
              />
              <span className="text-sm text-text-secondary">
                Forged in Gas Town
              </span>
              <Anvil className="w-4 h-4 text-text-tertiary" strokeWidth={1.5} />
            </div>
            <p className="text-xs text-text-tertiary text-center">
              &copy; 2026 My Release Notes &middot; A{' '}
              <a
                href="https://www.yourtinytools.com"
                target="_blank"
                rel="noopener noreferrer"
                className="hover:text-text-secondary transition-colors"
              >
                Tiny Tools
              </a>{' '}
              product
            </p>
            <p className="text-xs text-text-tertiary text-center">
              GitHub is a trademark of GitHub, Inc. This site is not affiliated
              with GitHub, Inc.
            </p>
          </div>
        </div>
      </Container>
    </footer>
  )
}
