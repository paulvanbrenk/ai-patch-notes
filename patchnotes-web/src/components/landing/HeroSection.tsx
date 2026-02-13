import { Link } from '@tanstack/react-router'
import { Button } from '../ui'
import { Container } from '../ui'
import { Logo } from './Logo'

export function HeroSection() {
  const handleScrollToFeatures = (e: React.MouseEvent<HTMLAnchorElement>) => {
    e.preventDefault()
    document.getElementById('features')?.scrollIntoView({ behavior: 'smooth' })
  }

  return (
    <section className="relative overflow-hidden bg-gradient-to-b from-brand-50 to-surface-secondary dark:from-surface-primary dark:to-surface-secondary">
      <Container>
        <div className="flex flex-col items-center text-center py-20 sm:py-28 lg:py-36">
          <Logo size={80} className="mb-8" />

          <h1 className="text-4xl sm:text-5xl lg:text-6xl font-bold text-text-primary tracking-tight max-w-3xl">
            Never miss a release that matters
          </h1>

          <p className="mt-6 text-lg sm:text-xl text-text-secondary max-w-2xl">
            AI-powered summaries of every GitHub release. Know what changed, why
            it matters, and whether you need to update — in seconds.
          </p>

          <div className="mt-10 flex flex-col sm:flex-row items-center gap-4">
            <Link to="/login">
              <Button size="lg" className="text-base px-8 py-3">
                Get Started Free
              </Button>
            </Link>
            <a href="#features" onClick={handleScrollToFeatures}>
              <Button
                variant="secondary"
                size="lg"
                className="text-base px-8 py-3"
              >
                See how it works
              </Button>
            </a>
          </div>
        </div>
      </Container>
    </section>
  )
}
