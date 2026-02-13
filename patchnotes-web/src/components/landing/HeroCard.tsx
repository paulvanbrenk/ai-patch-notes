import { useState } from 'react'
import { Link } from '@tanstack/react-router'
import {
  ChevronLeft,
  ChevronRight,
  X,
  Sparkles,
  Package,
  SlidersHorizontal,
  MonitorSmartphone,
  UserPlus,
  PackageSearch,
  Bell,
  Check,
  Zap,
  Shield,
  Clock,
} from 'lucide-react'
import { Card, Button } from '../ui'

const SLIDE_COUNT = 4

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

const features = [
  {
    icon: Sparkles,
    title: 'AI Summaries',
    description:
      'AI condenses changelogs into clear, actionable insights you can read in seconds.',
  },
  {
    icon: Package,
    title: 'Track Any Package',
    description:
      'Watch any public GitHub repo. Get notified the moment new versions drop.',
  },
  {
    icon: SlidersHorizontal,
    title: 'Smart Filtering',
    description:
      'Group by package, sort by date or name, and hide pre-releases you don\u2019t need.',
  },
  {
    icon: MonitorSmartphone,
    title: 'Works Everywhere',
    description:
      'Fully responsive with dark mode. Check releases from any device, any time.',
  },
]

const steps = [
  {
    icon: UserPlus,
    title: 'Sign up free',
    description: 'Create an account with just your email.',
  },
  {
    icon: PackageSearch,
    title: 'Pick your packages',
    description: 'Select the GitHub packages you want to track.',
  },
  {
    icon: Bell,
    title: 'Stay updated',
    description: 'Get a personalized feed with AI summaries.',
  },
]

const highlights = [
  {
    icon: Zap,
    title: 'AI Summaries',
    description: 'Changelogs condensed into clear, actionable insights.',
  },
  {
    icon: Shield,
    title: 'Always Free',
    description: 'Track up to 5 packages at no cost, forever.',
  },
  {
    icon: Clock,
    title: 'Weekly Digest',
    description: 'A curated summary of every release, delivered weekly.',
  },
]

// ---------------------------------------------------------------------------
// Slide Bodies (content below the fixed header)
// ---------------------------------------------------------------------------

function HeroBody() {
  return (
    <div className="flex flex-col items-center">
      <div className="flex flex-col sm:flex-row items-start sm:items-center justify-center gap-5 sm:gap-8">
        {highlights.map((h) => (
          <div
            key={h.title}
            className="flex items-start gap-3 sm:flex-col sm:items-center sm:text-center"
          >
            <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-full bg-brand-600 text-white">
              <h.icon className="h-4 w-4" strokeWidth={1.5} />
            </div>
            <div>
              <p className="text-sm font-medium text-text-primary">{h.title}</p>
              <p className="text-xs text-text-secondary max-w-[180px]">
                {h.description}
              </p>
            </div>
          </div>
        ))}
      </div>
      <Link to="/login" className="mt-3">
        <Button className="px-6 text-xs">Get Started Free</Button>
      </Link>
    </div>
  )
}

function FeaturesBody() {
  return (
    <div className="grid grid-cols-1 sm:grid-cols-2 gap-3 px-2">
      {features.map((f) => (
        <div key={f.title} className="flex items-start gap-3">
          <div className="flex h-9 w-9 shrink-0 items-center justify-center rounded-lg bg-brand-50 dark:bg-brand-950">
            <f.icon
              className="h-4 w-4 text-brand-600 dark:text-brand-400"
              strokeWidth={1.5}
            />
          </div>
          <div>
            <p className="text-sm font-medium text-text-primary">{f.title}</p>
            <p className="text-xs text-text-secondary">{f.description}</p>
          </div>
        </div>
      ))}
    </div>
  )
}

function HowItWorksBody() {
  return (
    <div className="flex flex-col items-center">
      <div className="flex flex-col sm:flex-row items-start sm:items-center justify-center gap-5 sm:gap-8">
        {steps.map((step, i) => (
          <div
            key={step.title}
            className="flex items-start gap-3 sm:flex-col sm:items-center sm:text-center"
          >
            <div className="relative flex h-10 w-10 shrink-0 items-center justify-center rounded-full bg-brand-600 text-white">
              <step.icon className="h-4 w-4" strokeWidth={1.5} />
              <span className="absolute -top-1 -right-1 flex h-4 w-4 items-center justify-center rounded-full bg-surface-primary border-2 border-brand-600 text-[9px] font-bold text-brand-600">
                {i + 1}
              </span>
            </div>
            <div>
              <p className="text-sm font-medium text-text-primary">
                {step.title}
              </p>
              <p className="text-xs text-text-secondary max-w-[180px]">
                {step.description}
              </p>
            </div>
          </div>
        ))}
      </div>
      <Link to="/login" className="mt-3">
        <Button variant="secondary" className="px-5 text-xs">
          Create your free account
        </Button>
      </Link>
    </div>
  )
}

function PricingBody() {
  return (
    <div className="grid grid-cols-1 sm:grid-cols-2 gap-2.5 max-w-lg mx-auto px-2">
      {/* Free */}
      <div className="rounded-lg border border-border-default bg-surface-secondary p-2.5">
        <p className="text-sm font-semibold text-text-primary mb-0.5">Free</p>
        <p className="text-lg font-bold text-text-primary">
          $0
          <span className="text-sm font-normal text-text-secondary">
            /forever
          </span>
        </p>
        <ul className="mt-2 space-y-1">
          {FREE_FEATURES.map((f) => (
            <li
              key={f}
              className="flex items-start gap-2 text-xs text-text-secondary"
            >
              <Check className="w-3.5 h-3.5 text-emerald-500 shrink-0 mt-0.5" />
              {f}
            </li>
          ))}
        </ul>
      </div>
      {/* Pro */}
      <div className="rounded-lg border-2 border-brand-500 dark:border-brand-400 bg-surface-secondary p-2.5 relative">
        <span className="absolute -top-2.5 left-1/2 -translate-x-1/2 inline-flex items-center gap-1 px-2 py-0.5 text-[10px] font-semibold bg-brand-500 text-white rounded-full">
          <Sparkles className="w-3 h-3" />
          Popular
        </span>
        <p className="text-sm font-semibold text-text-primary mb-0.5">Pro</p>
        <p className="text-lg font-bold text-text-primary">
          $20
          <span className="text-sm font-normal text-text-secondary">/year</span>
        </p>
        <ul className="mt-2 space-y-1">
          {PRO_FEATURES.map((f) => (
            <li
              key={f}
              className="flex items-start gap-2 text-xs text-text-secondary"
            >
              <Check className="w-3.5 h-3.5 text-brand-500 shrink-0 mt-0.5" />
              {f}
            </li>
          ))}
        </ul>
      </div>
    </div>
  )
}

// ---------------------------------------------------------------------------
// Slide definitions (title/subtitle rendered at fixed position in the card)
// ---------------------------------------------------------------------------

const slides = [
  {
    title: 'Never miss a release that matters',
    subtitle: 'AI-powered summaries of every GitHub release.',
    Body: HeroBody,
  },
  {
    title: 'Everything you need to stay current',
    subtitle:
      'Stop scrolling through changelogs. Get the information you need.',
    Body: FeaturesBody,
  },
  {
    title: 'How it works',
    subtitle: 'Get started in under a minute — no setup required.',
    Body: HowItWorksBody,
  },
  {
    title: 'Simple pricing',
    subtitle: 'Start free and upgrade when you need more. Cancel anytime.',
    Body: PricingBody,
  },
]

// ---------------------------------------------------------------------------
// Main Component
// ---------------------------------------------------------------------------

export function HeroCard({ onDismiss }: { onDismiss: () => void }) {
  const [index, setIndex] = useState(0)

  const prev = () => setIndex((i) => (i - 1 + SLIDE_COUNT) % SLIDE_COUNT)
  const next = () => setIndex((i) => (i + 1) % SLIDE_COUNT)

  const slide = slides[index]

  return (
    <Card padding="none" className="relative mb-6 overflow-hidden">
      {/* Dismiss button */}
      <button
        onClick={onDismiss}
        className="absolute top-2 right-2 z-10 p-1 rounded-md text-text-tertiary hover:text-text-primary hover:bg-surface-tertiary transition-colors"
        aria-label="Dismiss hero"
      >
        <X className="w-4 h-4" />
      </button>

      {/* Prev / Next arrows */}
      <button
        onClick={prev}
        className="absolute left-1 top-1/2 -translate-y-1/2 z-10 p-1 rounded-full text-text-tertiary hover:text-text-primary hover:bg-surface-tertiary transition-colors"
        aria-label="Previous slide"
      >
        <ChevronLeft className="w-5 h-5" />
      </button>
      <button
        onClick={next}
        className="absolute right-1 top-1/2 -translate-y-1/2 z-10 p-1 rounded-full text-text-tertiary hover:text-text-primary hover:bg-surface-tertiary transition-colors"
        aria-label="Next slide"
      >
        <ChevronRight className="w-5 h-5" />
      </button>

      <div className="px-8 pt-3 pb-1">
        {/* Fixed-position header */}
        <div className="text-center mb-3">
          <h3 className="text-base font-semibold text-text-primary">
            {slide.title}
          </h3>
          <p className="text-xs text-text-tertiary mt-0.5">{slide.subtitle}</p>
        </div>

        {/* Slide body (fixed height so arrows don't shift) */}
        <div className="h-[190px] flex items-center justify-center">
          <slide.Body />
        </div>
      </div>

      {/* Dot navigation */}
      <div className="flex items-center justify-center gap-1.5 pb-2">
        {Array.from({ length: SLIDE_COUNT }, (_, i) => (
          <button
            key={i}
            onClick={() => setIndex(i)}
            aria-label={`Go to slide ${i + 1}`}
            className={`w-2 h-2 rounded-full transition-colors ${
              i === index
                ? 'bg-brand-600 dark:bg-brand-400'
                : 'bg-border-default hover:bg-text-tertiary'
            }`}
          />
        ))}
      </div>
    </Card>
  )
}
