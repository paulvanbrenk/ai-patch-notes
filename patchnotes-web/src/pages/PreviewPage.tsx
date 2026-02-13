import { Link } from '@tanstack/react-router'
import { Header, HeaderTitle } from '../components/ui'
import { ThemeToggle } from '../components/theme'
import { UserMenu } from '../components/auth'
import { Logo } from '../components/landing/Logo'
import { HeroSection } from '../components/landing/HeroSection'
import { FeaturesSection } from '../components/landing/FeaturesSection'
import { HowItWorksSection } from '../components/landing/HowItWorksSection'
import { PricingPreview } from '../components/landing/PricingPreview'

export function PreviewPage() {
  return (
    <div className="min-h-screen bg-surface-secondary">
      <Header>
        <Link to="/preview" className="flex items-center gap-3">
          <Logo size={32} />
          <div>
            <HeaderTitle className="!text-lg leading-tight">
              My Release Notes
            </HeaderTitle>
            <span className="text-xs text-text-tertiary">by Tiny Tools</span>
          </div>
        </Link>
        <div className="flex items-center gap-2">
          <ThemeToggle />
          <UserMenu />
        </div>
      </Header>

      <main>
        <HeroSection />
        <FeaturesSection />
        <HowItWorksSection />
        <PricingPreview />
      </main>
    </div>
  )
}
