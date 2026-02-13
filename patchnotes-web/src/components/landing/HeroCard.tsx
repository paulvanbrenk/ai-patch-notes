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
} from 'lucide-react'
import { Card, Button } from '../ui'
import { Logo } from './Logo'

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
    description: 'Understand what changed at a glance.',
  },
  {
    icon: Package,
    title: 'Track Any Package',
    description: 'Watch releases from any GitHub repo.',
  },
  {
    icon: SlidersHorizontal,
    title: 'Smart Filtering',
    description: 'Group, sort, and hide pre-releases.',
  },
  {
    icon: MonitorSmartphone,
    title: 'Works Everywhere',
    description: 'Mobile-ready with full dark mode.',
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

// ---------------------------------------------------------------------------
// Slides
// ---------------------------------------------------------------------------

function HeroSlide() {
  return (
    <div className="flex flex-col items-center text-center px-4 py-2">
      <Logo size={56} className="mb-4" />
      <h2 className="text-2xl sm:text-3xl font-bold text-text-primary tracking-tight">
        Never miss a release that matters
      </h2>
      <p className="mt-3 text-sm sm:text-base text-text-secondary max-w-md">
        AI-powered summaries of every GitHub release. Know what changed, why it
        matters, and whether you need to update — in seconds.
      </p>
      <Link to="/login" className="mt-6">
        <Button size="lg" className="px-8">
          Get Started Free
        </Button>
      </Link>
    </div>
  )
}

function FeaturesSlide() {
  return (
    <div className="px-2 py-2">
      <h3 className="text-lg font-semibold text-text-primary text-center mb-4">
        Everything you need to stay current
      </h3>
      <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
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
    </div>
  )
}

function HowItWorksSlide() {
  return (
    <div className="px-2 py-2">
      <h3 className="text-lg font-semibold text-text-primary text-center mb-5">
        How it works
      </h3>
      <div className="flex flex-col sm:flex-row items-start sm:items-center justify-center gap-6 sm:gap-8">
        {steps.map((step, i) => (
          <div
            key={step.title}
            className="flex items-start gap-3 sm:flex-col sm:items-center sm:text-center"
          >
            <div className="relative flex h-12 w-12 shrink-0 items-center justify-center rounded-full bg-brand-600 text-white">
              <step.icon className="h-5 w-5" strokeWidth={1.5} />
              <span className="absolute -top-1.5 -right-1.5 flex h-5 w-5 items-center justify-center rounded-full bg-surface-primary border-2 border-brand-600 text-[10px] font-bold text-brand-600">
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
    </div>
  )
}

function PricingSlide() {
  return (
    <div className="px-2 py-2">
      <h3 className="text-lg font-semibold text-text-primary text-center mb-4">
        Simple pricing
      </h3>
      <div className="grid grid-cols-1 sm:grid-cols-2 gap-4 max-w-lg mx-auto">
        {/* Free */}
        <div className="rounded-lg border border-border-default bg-surface-secondary p-4">
          <p className="font-semibold text-text-primary mb-1">Free</p>
          <p className="text-2xl font-bold text-text-primary">
            $0
            <span className="text-sm font-normal text-text-secondary">
              /forever
            </span>
          </p>
          <ul className="mt-3 space-y-1.5">
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
        <div className="rounded-lg border-2 border-brand-500 dark:border-brand-400 bg-surface-secondary p-4 relative">
          <span className="absolute -top-2.5 left-1/2 -translate-x-1/2 inline-flex items-center gap-1 px-2 py-0.5 text-[10px] font-semibold bg-brand-500 text-white rounded-full">
            <Sparkles className="w-3 h-3" />
            Popular
          </span>
          <p className="font-semibold text-text-primary mb-1">Pro</p>
          <p className="text-2xl font-bold text-text-primary">
            $20
            <span className="text-sm font-normal text-text-secondary">
              /year
            </span>
          </p>
          <ul className="mt-3 space-y-1.5">
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
    </div>
  )
}

// ---------------------------------------------------------------------------
// Main Component
// ---------------------------------------------------------------------------

const slides = [HeroSlide, FeaturesSlide, HowItWorksSlide, PricingSlide]

export function HeroCard({ onDismiss }: { onDismiss: () => void }) {
  const [index, setIndex] = useState(0)

  const prev = () => setIndex((i) => (i - 1 + SLIDE_COUNT) % SLIDE_COUNT)
  const next = () => setIndex((i) => (i + 1) % SLIDE_COUNT)

  const Slide = slides[index]

  return (
    <Card className="relative mb-6 overflow-hidden">
      {/* Dismiss button */}
      <button
        onClick={onDismiss}
        className="absolute top-3 right-3 z-10 p-1 rounded-md text-text-tertiary hover:text-text-primary hover:bg-surface-tertiary transition-colors"
        aria-label="Dismiss hero"
      >
        <X className="w-4 h-4" />
      </button>

      {/* Prev / Next arrows */}
      <button
        onClick={prev}
        className="absolute left-2 top-1/2 -translate-y-1/2 z-10 p-1 rounded-full text-text-tertiary hover:text-text-primary hover:bg-surface-tertiary transition-colors"
        aria-label="Previous slide"
      >
        <ChevronLeft className="w-5 h-5" />
      </button>
      <button
        onClick={next}
        className="absolute right-2 top-1/2 -translate-y-1/2 z-10 p-1 rounded-full text-text-tertiary hover:text-text-primary hover:bg-surface-tertiary transition-colors"
        aria-label="Next slide"
      >
        <ChevronRight className="w-5 h-5" />
      </button>

      {/* Slide content */}
      <div className="px-8 py-4 min-h-[260px] flex items-center justify-center">
        <Slide />
      </div>

      {/* Dot navigation */}
      <div className="flex items-center justify-center gap-1.5 pb-4">
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
