import { UserPlus, PackageSearch, Bell } from 'lucide-react'
import { Container } from '../ui'

const steps = [
  {
    icon: UserPlus,
    title: 'Sign up free',
    description:
      'Create an account with just your email. No credit card required.',
  },
  {
    icon: PackageSearch,
    title: 'Pick your packages',
    description: 'Search and select the GitHub packages you want to track.',
  },
  {
    icon: Bell,
    title: 'Stay updated',
    description:
      'Get a personalized feed with AI summaries. Upgrade for email digests.',
  },
]

export function HowItWorksSection() {
  return (
    <section className="py-20 sm:py-28 bg-surface-primary">
      <Container>
        <div className="text-center mb-16">
          <h2 className="text-3xl sm:text-4xl font-bold text-text-primary">
            How it works
          </h2>
          <p className="mt-4 text-lg text-text-secondary max-w-2xl mx-auto">
            Get started in under a minute. No setup, no configuration.
          </p>
        </div>

        <div className="relative max-w-4xl mx-auto">
          {/* Connecting line (desktop only) */}
          <div
            className="hidden sm:block absolute top-12 left-[calc(16.67%+20px)] right-[calc(16.67%+20px)] border-t-2 border-dashed border-border-default"
            aria-hidden="true"
          />

          <div className="grid sm:grid-cols-3 gap-12 sm:gap-8">
            {steps.map((step, index) => (
              <div
                key={step.title}
                className="relative flex flex-col items-center text-center"
              >
                <div className="relative z-10 flex h-16 w-16 items-center justify-center rounded-full bg-brand-600 text-white shadow-md mb-6">
                  <step.icon className="h-7 w-7" strokeWidth={1.5} />
                  <span className="absolute -top-2 -right-2 flex h-6 w-6 items-center justify-center rounded-full bg-surface-primary border-2 border-brand-600 text-xs font-bold text-brand-600">
                    {index + 1}
                  </span>
                </div>
                <h3 className="text-lg font-semibold text-text-primary mb-2">
                  {step.title}
                </h3>
                <p className="text-text-secondary">{step.description}</p>
              </div>
            ))}
          </div>
        </div>
      </Container>
    </section>
  )
}
