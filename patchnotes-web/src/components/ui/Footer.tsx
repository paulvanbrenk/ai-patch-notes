import { type HTMLAttributes, forwardRef } from 'react'
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
          <div className="h-12 flex items-center justify-center">
            <span className="text-sm text-text-secondary">
              Forged in Gas Town
            </span>
          </div>
        </Container>
      </footer>
    )
  }
)

Footer.displayName = 'Footer'
