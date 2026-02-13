import { useStytchUser } from '@stytch/react'
import { Link } from '@tanstack/react-router'
import { Check, Sparkles } from 'lucide-react'
import { Button, Card, Container } from '../ui'
import { useSubscriptionStore } from '../../stores/subscriptionStore'

const FREE_FEATURES = [
  'Track up to 5 packages',
  'AI-powered release summaries',
  'Version grouping & filtering',
  'Dark mode support',
]

const PRO_FEATURES = [
  'Everything in Free',
  'Track unlimited packages',
  'No advertisements',
  'Weekly email highlights',
]

export function PricingPreview() {
  const { user, isInitialized } = useStytchUser()
  const { isPro, isLoading, startCheckout } = useSubscriptionStore()

  const handleUpgrade = () => {
    if (!user) {
      window.location.href = '/login'
      return
    }
    startCheckout()
  }

  return (
    <section className="py-20 sm:py-28 bg-surface-secondary">
      <Container>
        <div className="text-center mb-12">
          <h2 className="text-3xl sm:text-4xl font-bold text-text-primary">
            Simple, transparent pricing
          </h2>
          <p className="mt-4 text-lg text-text-secondary max-w-2xl mx-auto">
            Start free and upgrade when you need more. No hidden fees, cancel
            anytime.
          </p>
        </div>

        <div className="grid md:grid-cols-2 gap-8 max-w-3xl mx-auto">
          {/* Free Tier */}
          <Card padding="lg">
            <div className="mb-6">
              <h3 className="text-xl font-semibold text-text-primary mb-2">
                Free
              </h3>
              <div className="flex items-baseline gap-1">
                <span className="text-4xl font-bold text-text-primary">$0</span>
                <span className="text-text-secondary">/forever</span>
              </div>
            </div>

            <ul className="space-y-3 mb-8">
              {FREE_FEATURES.map((feature) => (
                <li key={feature} className="flex items-start gap-3">
                  <Check className="w-5 h-5 text-emerald-500 flex-shrink-0 mt-0.5" />
                  <span className="text-text-secondary">{feature}</span>
                </li>
              ))}
            </ul>

            {!isInitialized ? (
              <Button variant="secondary" size="lg" className="w-full" disabled>
                Loading...
              </Button>
            ) : !user ? (
              <Link to="/login" className="block">
                <Button variant="secondary" size="lg" className="w-full">
                  Get Started
                </Button>
              </Link>
            ) : (
              <Button variant="secondary" size="lg" className="w-full" disabled>
                Current Plan
              </Button>
            )}
          </Card>

          {/* Pro Tier */}
          <Card
            padding="lg"
            className="relative border-brand-500 dark:border-brand-400 border-2"
          >
            <div className="absolute -top-3 left-1/2 -translate-x-1/2">
              <span className="inline-flex items-center gap-1.5 px-3 py-1 text-xs font-semibold bg-brand-500 text-white rounded-full">
                <Sparkles className="w-3.5 h-3.5" />
                Most Popular
              </span>
            </div>

            <div className="mb-6">
              <h3 className="text-xl font-semibold text-text-primary mb-2">
                Pro
              </h3>
              <div className="flex items-baseline gap-1">
                <span className="text-4xl font-bold text-text-primary">
                  $20
                </span>
                <span className="text-text-secondary">/year</span>
              </div>
              <p className="text-sm text-text-tertiary mt-1">
                Less than $2/month
              </p>
            </div>

            <ul className="space-y-3 mb-8">
              {PRO_FEATURES.map((feature) => (
                <li key={feature} className="flex items-start gap-3">
                  <Check className="w-5 h-5 text-brand-500 flex-shrink-0 mt-0.5" />
                  <span className="text-text-secondary">{feature}</span>
                </li>
              ))}
            </ul>

            {!isInitialized ? (
              <Button size="lg" className="w-full" disabled>
                Loading...
              </Button>
            ) : isPro ? (
              <Button variant="secondary" size="lg" className="w-full" disabled>
                Current Plan
              </Button>
            ) : (
              <Button
                size="lg"
                className="w-full"
                onClick={handleUpgrade}
                disabled={isLoading}
              >
                {isLoading ? 'Loading...' : 'Upgrade to Pro'}
              </Button>
            )}
          </Card>
        </div>

        <div className="text-center mt-8">
          <Link
            to="/pricing"
            className="text-sm text-brand-600 hover:text-brand-700 dark:text-brand-400 dark:hover:text-brand-300 font-medium"
          >
            View full pricing details &rarr;
          </Link>
        </div>
      </Container>
    </section>
  )
}
