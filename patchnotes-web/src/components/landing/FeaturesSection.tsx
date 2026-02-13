import {
  Sparkles,
  Package,
  SlidersHorizontal,
  MonitorSmartphone,
} from 'lucide-react'
import { Card } from '../ui'
import { Container } from '../ui'

const features = [
  {
    icon: Sparkles,
    title: 'AI-Powered Summaries',
    description:
      'Understand what changed at a glance. AI condenses release notes into clear, actionable insights.',
  },
  {
    icon: Package,
    title: 'Track Any Package',
    description:
      'Watch releases from any public GitHub repository. Get notified the moment new versions drop.',
  },
  {
    icon: SlidersHorizontal,
    title: 'Smart Filtering',
    description:
      'Group by package, sort by date or name, hide pre-releases. See only what matters.',
  },
  {
    icon: MonitorSmartphone,
    title: 'Works Everywhere',
    description:
      'Mobile-ready with full dark mode support. Check releases from any device, any time.',
  },
]

export function FeaturesSection() {
  return (
    <section id="features" className="py-20 sm:py-28 bg-surface-secondary">
      <Container>
        <div className="text-center mb-12">
          <h2 className="text-3xl sm:text-4xl font-bold text-text-primary">
            Everything you need to stay current
          </h2>
          <p className="mt-4 text-lg text-text-secondary max-w-2xl mx-auto">
            Stop scrolling through changelogs. Get the information you need, the
            way you need it.
          </p>
        </div>

        <div className="grid sm:grid-cols-2 gap-6 max-w-4xl mx-auto">
          {features.map((feature) => (
            <Card key={feature.title} padding="lg">
              <div className="flex items-center gap-3 mb-3">
                <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-brand-50 dark:bg-brand-950">
                  <feature.icon
                    className="h-5 w-5 text-brand-600 dark:text-brand-400"
                    strokeWidth={1.5}
                  />
                </div>
                <h3 className="text-lg font-semibold text-text-primary">
                  {feature.title}
                </h3>
              </div>
              <p className="text-text-secondary">{feature.description}</p>
            </Card>
          ))}
        </div>
      </Container>
    </section>
  )
}
