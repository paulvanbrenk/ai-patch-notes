import { type HTMLAttributes, forwardRef } from 'react'
import { Hammer, Anvil } from 'lucide-react'
import { Container } from './Container'

type FooterProps = HTMLAttributes<HTMLElement>

export const Footer = forwardRef<HTMLElement, FooterProps>(
  ({ className = '', ...props }, ref) => {
    return (
      <footer
        ref={ref}
        className={`
          mt-auto
          bg-surface-primary/80
          backdrop-blur-md
          border-t border-border-default
          ${className}
        `}
        {...props}
      >
        <Container>
          <div className="py-3 flex flex-col items-center gap-1.5">
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
            <p className="text-[10px] text-text-tertiary text-center">
              GitHub is a trademark of GitHub, Inc. This site is not affiliated
              with GitHub, Inc.
            </p>
          </div>
        </Container>
      </footer>
    )
  }
)

Footer.displayName = 'Footer'
