import {
  Header,
  HeaderTitle,
  Container,
  Button,
  Input,
  Card,
  CardHeader,
  CardTitle,
  CardContent,
  Badge,
} from '../components/ui'
import { VersionBadge, ReleaseCard, PackageCard } from '../components/releases'

function Section({
  title,
  children,
}: {
  title: string
  children: React.ReactNode
}) {
  return (
    <section className="mb-12">
      <h2 className="text-2xl font-semibold text-text-primary mb-6 pb-2 border-b border-border-default">
        {title}
      </h2>
      {children}
    </section>
  )
}

function ColorSwatch({
  name,
  className,
  textClass = 'text-text-primary',
}: {
  name: string
  className: string
  textClass?: string
}) {
  return (
    <div className="flex flex-col gap-2">
      <div className={`w-full h-16 rounded-lg ${className}`} />
      <span className={`text-xs font-mono ${textClass}`}>{name}</span>
    </div>
  )
}

export function DesignSystem() {
  return (
    <div className="min-h-screen bg-surface-secondary">
      <Header>
        <HeaderTitle>Design System</HeaderTitle>
        <Button variant="secondary" size="sm">
          Back to App
        </Button>
      </Header>

      <main className="py-8">
        <Container>
          {/* Introduction */}
          <div className="mb-12">
            <h1 className="text-3xl font-bold text-text-primary mb-4">
              PatchNotes Design System
            </h1>
            <p className="text-text-secondary text-lg max-w-2xl">
              A comprehensive visual language for the PatchNotes application.
              Built with Tailwind CSS v4 using OKLCH colors for perceptual
              uniformity.
            </p>
          </div>

          {/* Colors */}
          <Section title="Colors">
            <div className="space-y-8">
              {/* Brand Colors */}
              <div>
                <h3 className="text-lg font-medium text-text-primary mb-4">
                  Brand
                </h3>
                <div className="grid grid-cols-5 md:grid-cols-10 gap-4">
                  <ColorSwatch name="50" className="bg-brand-50" />
                  <ColorSwatch name="100" className="bg-brand-100" />
                  <ColorSwatch name="200" className="bg-brand-200" />
                  <ColorSwatch name="300" className="bg-brand-300" />
                  <ColorSwatch name="400" className="bg-brand-400" />
                  <ColorSwatch
                    name="500"
                    className="bg-brand-500"
                    textClass="text-white"
                  />
                  <ColorSwatch
                    name="600"
                    className="bg-brand-600"
                    textClass="text-white"
                  />
                  <ColorSwatch
                    name="700"
                    className="bg-brand-700"
                    textClass="text-white"
                  />
                  <ColorSwatch
                    name="800"
                    className="bg-brand-800"
                    textClass="text-white"
                  />
                  <ColorSwatch
                    name="900"
                    className="bg-brand-900"
                    textClass="text-white"
                  />
                </div>
              </div>

              {/* Semantic Colors */}
              <div>
                <h3 className="text-lg font-medium text-text-primary mb-4">
                  Release Types
                </h3>
                <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
                  <div className="space-y-2">
                    <ColorSwatch name="Major" className="bg-major" />
                    <ColorSwatch
                      name="Major Muted"
                      className="bg-major-muted"
                    />
                  </div>
                  <div className="space-y-2">
                    <ColorSwatch name="Minor" className="bg-minor" />
                    <ColorSwatch
                      name="Minor Muted"
                      className="bg-minor-muted"
                    />
                  </div>
                  <div className="space-y-2">
                    <ColorSwatch name="Patch" className="bg-patch" />
                    <ColorSwatch
                      name="Patch Muted"
                      className="bg-patch-muted"
                    />
                  </div>
                  <div className="space-y-2">
                    <ColorSwatch name="Prerelease" className="bg-prerelease" />
                    <ColorSwatch
                      name="Prerelease Muted"
                      className="bg-prerelease-muted"
                    />
                  </div>
                </div>
              </div>

              {/* Surface Colors */}
              <div>
                <h3 className="text-lg font-medium text-text-primary mb-4">
                  Surfaces
                </h3>
                <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
                  <ColorSwatch
                    name="Primary"
                    className="bg-surface-primary border border-border-default"
                  />
                  <ColorSwatch
                    name="Secondary"
                    className="bg-surface-secondary border border-border-default"
                  />
                  <ColorSwatch
                    name="Tertiary"
                    className="bg-surface-tertiary border border-border-default"
                  />
                  <ColorSwatch
                    name="Inverse"
                    className="bg-surface-inverse"
                    textClass="text-white"
                  />
                </div>
              </div>
            </div>
          </Section>

          {/* Typography */}
          <Section title="Typography">
            <Card>
              <div className="space-y-6">
                <div>
                  <span className="text-xs text-text-tertiary uppercase tracking-wide">
                    4xl / 2.25rem
                  </span>
                  <p className="text-4xl font-bold">Display Heading</p>
                </div>
                <div>
                  <span className="text-xs text-text-tertiary uppercase tracking-wide">
                    3xl / 1.875rem
                  </span>
                  <p className="text-3xl font-semibold">Page Title</p>
                </div>
                <div>
                  <span className="text-xs text-text-tertiary uppercase tracking-wide">
                    2xl / 1.5rem
                  </span>
                  <p className="text-2xl font-semibold">Section Heading</p>
                </div>
                <div>
                  <span className="text-xs text-text-tertiary uppercase tracking-wide">
                    xl / 1.25rem
                  </span>
                  <p className="text-xl font-medium">Card Title</p>
                </div>
                <div>
                  <span className="text-xs text-text-tertiary uppercase tracking-wide">
                    lg / 1.125rem
                  </span>
                  <p className="text-lg">Large Body Text</p>
                </div>
                <div>
                  <span className="text-xs text-text-tertiary uppercase tracking-wide">
                    base / 1rem
                  </span>
                  <p className="text-base">Default body text for paragraphs.</p>
                </div>
                <div>
                  <span className="text-xs text-text-tertiary uppercase tracking-wide">
                    sm / 0.875rem
                  </span>
                  <p className="text-sm text-text-secondary">
                    Secondary text and captions.
                  </p>
                </div>
                <div>
                  <span className="text-xs text-text-tertiary uppercase tracking-wide">
                    xs / 0.75rem
                  </span>
                  <p className="text-xs text-text-tertiary">
                    Labels and metadata.
                  </p>
                </div>
                <div>
                  <span className="text-xs text-text-tertiary uppercase tracking-wide">
                    Monospace
                  </span>
                  <p className="font-mono text-sm">
                    v19.0.0 - Code and versions
                  </p>
                </div>
              </div>
            </Card>
          </Section>

          {/* Buttons */}
          <Section title="Buttons">
            <Card>
              <div className="space-y-6">
                <div>
                  <h4 className="text-sm font-medium text-text-secondary mb-3">
                    Variants
                  </h4>
                  <div className="flex flex-wrap gap-4">
                    <Button variant="primary">Primary</Button>
                    <Button variant="secondary">Secondary</Button>
                    <Button variant="ghost">Ghost</Button>
                  </div>
                </div>
                <div>
                  <h4 className="text-sm font-medium text-text-secondary mb-3">
                    Sizes
                  </h4>
                  <div className="flex flex-wrap items-center gap-4">
                    <Button size="sm">Small</Button>
                    <Button size="md">Medium</Button>
                    <Button size="lg">Large</Button>
                  </div>
                </div>
                <div>
                  <h4 className="text-sm font-medium text-text-secondary mb-3">
                    States
                  </h4>
                  <div className="flex flex-wrap gap-4">
                    <Button>Default</Button>
                    <Button disabled>Disabled</Button>
                  </div>
                </div>
              </div>
            </Card>
          </Section>

          {/* Badges */}
          <Section title="Badges">
            <Card>
              <div className="space-y-6">
                <div>
                  <h4 className="text-sm font-medium text-text-secondary mb-3">
                    Basic Variants
                  </h4>
                  <div className="flex flex-wrap gap-3">
                    <Badge>Default</Badge>
                    <Badge variant="major">Major</Badge>
                    <Badge variant="minor">Minor</Badge>
                    <Badge variant="patch">Patch</Badge>
                    <Badge variant="prerelease">Prerelease</Badge>
                  </div>
                </div>
                <div>
                  <h4 className="text-sm font-medium text-text-secondary mb-3">
                    Version Badges
                  </h4>
                  <div className="flex flex-wrap gap-3">
                    <VersionBadge version="v1.0.0" />
                    <VersionBadge version="v2.1.0" />
                    <VersionBadge version="v2.1.5" />
                    <VersionBadge version="v3.0.0-beta.1" />
                    <VersionBadge version="v4.0.0-rc.1" />
                  </div>
                </div>
              </div>
            </Card>
          </Section>

          {/* Inputs */}
          <Section title="Form Inputs">
            <Card>
              <div className="space-y-6 max-w-md">
                <Input placeholder="Default input" />
                <Input label="With Label" placeholder="Enter text..." />
                <Input
                  label="With Error"
                  error="This field is required"
                  placeholder="Invalid input"
                />
                <Input placeholder="Disabled input" disabled />
              </div>
            </Card>
          </Section>

          {/* Cards */}
          <Section title="Cards">
            <div className="space-y-6">
              <div>
                <h4 className="text-sm font-medium text-text-secondary mb-3">
                  Basic Card
                </h4>
                <Card>
                  <CardHeader>
                    <CardTitle>Card Title</CardTitle>
                    <Badge>Label</Badge>
                  </CardHeader>
                  <CardContent>
                    <p>
                      Card content goes here. Cards provide a container for
                      related content and actions.
                    </p>
                  </CardContent>
                </Card>
              </div>

              <div>
                <h4 className="text-sm font-medium text-text-secondary mb-3">
                  Padding Variants
                </h4>
                <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                  <Card padding="sm">
                    <p className="text-sm">Small padding (p-4)</p>
                  </Card>
                  <Card padding="md">
                    <p className="text-sm">Medium padding (p-5)</p>
                  </Card>
                  <Card padding="lg">
                    <p className="text-sm">Large padding (p-6)</p>
                  </Card>
                </div>
              </div>
            </div>
          </Section>

          {/* Domain Components */}
          <Section title="Release Components">
            <div className="space-y-6">
              <div>
                <h4 className="text-sm font-medium text-text-secondary mb-3">
                  Package Card
                </h4>
                <div className="max-w-md">
                  <PackageCard
                    npmName="react"
                    githubOwner="facebook"
                    githubRepo="react"
                    releaseCount={127}
                    lastFetchedAt="2026-01-17T10:30:00Z"
                  />
                </div>
              </div>

              <div>
                <h4 className="text-sm font-medium text-text-secondary mb-3">
                  Release Card
                </h4>
                <ReleaseCard
                  tag="v19.0.0"
                  title="React 19"
                  body="This major release includes Actions, new hooks like useActionState and useOptimistic, and significant improvements to ref handling."
                  publishedAt="2026-01-10T14:00:00Z"
                  htmlUrl="https://github.com/facebook/react/releases/tag/v19.0.0"
                />
              </div>
            </div>
          </Section>

          {/* Spacing */}
          <Section title="Spacing Scale">
            <Card>
              <div className="space-y-4">
                {[1, 2, 3, 4, 5, 6, 8, 10, 12, 16].map((n) => (
                  <div key={n} className="flex items-center gap-4">
                    <span className="w-12 text-sm font-mono text-text-secondary">
                      {n}
                    </span>
                    <div
                      className="h-4 bg-brand-500 rounded"
                      style={{ width: `${n * 0.25}rem` }}
                    />
                    <span className="text-xs text-text-tertiary">
                      {n * 0.25}rem / {n * 4}px
                    </span>
                  </div>
                ))}
              </div>
            </Card>
          </Section>

          {/* Shadows */}
          <Section title="Shadows">
            <div className="grid grid-cols-1 md:grid-cols-3 gap-8">
              <div className="space-y-2">
                <div className="bg-surface-primary p-8 rounded-lg shadow-sm" />
                <p className="text-sm text-text-secondary text-center">
                  shadow-sm
                </p>
              </div>
              <div className="space-y-2">
                <div className="bg-surface-primary p-8 rounded-lg shadow-md" />
                <p className="text-sm text-text-secondary text-center">
                  shadow-md
                </p>
              </div>
              <div className="space-y-2">
                <div className="bg-surface-primary p-8 rounded-lg shadow-lg" />
                <p className="text-sm text-text-secondary text-center">
                  shadow-lg
                </p>
              </div>
            </div>
          </Section>

          {/* Border Radius */}
          <Section title="Border Radius">
            <div className="grid grid-cols-2 md:grid-cols-5 gap-8">
              {[
                { name: 'sm', class: 'rounded-sm' },
                { name: 'md', class: 'rounded-md' },
                { name: 'lg', class: 'rounded-lg' },
                { name: 'xl', class: 'rounded-xl' },
                { name: 'full', class: 'rounded-full' },
              ].map(({ name, class: cls }) => (
                <div key={name} className="space-y-2">
                  <div className={`bg-brand-500 w-16 h-16 ${cls} mx-auto`} />
                  <p className="text-sm text-text-secondary text-center">
                    {name}
                  </p>
                </div>
              ))}
            </div>
          </Section>
        </Container>
      </main>
    </div>
  )
}
