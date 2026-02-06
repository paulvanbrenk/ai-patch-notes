/* eslint-disable react-refresh/only-export-components */
import { createFileRoute, Link } from '@tanstack/react-router'
import { useEffect } from 'react'
import { CheckCircle } from 'lucide-react'
import { Header, HeaderTitle, Container, Button, Card } from '../components/ui'
import { ThemeToggle } from '../components/theme'
import { UserMenu } from '../components/auth'
import { useSubscriptionStore } from '../stores/subscriptionStore'

function SubscriptionSuccess() {
  const { checkSubscription } = useSubscriptionStore()

  // Refresh subscription status on mount
  useEffect(() => {
    checkSubscription()
  }, [checkSubscription])

  return (
    <div className="min-h-screen bg-surface-secondary">
      <Header>
        <HeaderTitle>Patch Notes</HeaderTitle>
        <div className="flex items-center gap-2">
          <ThemeToggle />
          <UserMenu />
        </div>
      </Header>

      <main className="py-16">
        <Container>
          <Card className="max-w-lg mx-auto text-center py-12">
            <div className="w-16 h-16 mx-auto mb-6 rounded-full bg-emerald-100 dark:bg-emerald-900/30 flex items-center justify-center">
              <CheckCircle className="w-8 h-8 text-emerald-600 dark:text-emerald-400" />
            </div>

            <h1 className="text-2xl font-bold text-text-primary mb-3">
              Welcome to PatchNotes Pro!
            </h1>

            <p className="text-text-secondary mb-8">
              Thank you for subscribing. You now have access to unlimited
              package tracking and all Pro features.
            </p>

            <Link to="/">
              <Button size="lg">Start Exploring</Button>
            </Link>
          </Card>
        </Container>
      </main>
    </div>
  )
}

export const Route = createFileRoute('/subscription-success')({
  component: SubscriptionSuccess,
})
