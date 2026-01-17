using Microsoft.EntityFrameworkCore;

namespace PatchNotes.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(PatchNotesDbContext context)
    {
        if (await context.Packages.AnyAsync())
        {
            return; // Already seeded
        }

        var now = DateTime.UtcNow;
        var packages = GetSamplePackages(now);

        context.Packages.AddRange(packages);
        await context.SaveChangesAsync();
    }

    private static List<Package> GetSamplePackages(DateTime now)
    {
        return
        [
            new Package
            {
                NpmName = "react",
                GithubOwner = "facebook",
                GithubRepo = "react",
                CreatedAt = now,
                LastFetchedAt = now,
                Releases =
                [
                    new Release
                    {
                        Tag = "v19.0.0",
                        Title = "React 19",
                        Body = """
                            ## What's New in React 19

                            ### Actions
                            Actions are functions that handle data mutations and state updates. They can be used with `useTransition` and the new `useActionState` hook.

                            ### New Hooks
                            - `useActionState` - manage state of async actions
                            - `useFormStatus` - read form status from within form components
                            - `useOptimistic` - optimistically update UI while async request is in flight

                            ### Server Components
                            Server Components are now stable and allow rendering components on the server.

                            ### Document Metadata
                            Built-in support for `<title>`, `<meta>`, and `<link>` tags in components.
                            """,
                        PublishedAt = new DateTime(2024, 12, 5, 0, 0, 0, DateTimeKind.Utc),
                        FetchedAt = now
                    },
                    new Release
                    {
                        Tag = "v18.3.1",
                        Title = "React 18.3.1",
                        Body = """
                            ## Bug Fixes
                            - Fixed a regression in `useSyncExternalStore` that caused incorrect behavior with concurrent features
                            - Fixed memory leak in server rendering
                            """,
                        PublishedAt = new DateTime(2024, 4, 26, 0, 0, 0, DateTimeKind.Utc),
                        FetchedAt = now
                    },
                    new Release
                    {
                        Tag = "v18.2.0",
                        Title = "React 18.2.0",
                        Body = """
                            ## Improvements
                            - Allow `Suspense` to be used as a server component
                            - Fix edge case with `useSyncExternalStore`
                            """,
                        PublishedAt = new DateTime(2022, 6, 14, 0, 0, 0, DateTimeKind.Utc),
                        FetchedAt = now
                    }
                ]
            },
            new Package
            {
                NpmName = "vue",
                GithubOwner = "vuejs",
                GithubRepo = "core",
                CreatedAt = now,
                LastFetchedAt = now,
                Releases =
                [
                    new Release
                    {
                        Tag = "v3.5.13",
                        Title = "Vue 3.5.13",
                        Body = """
                            ## Bug Fixes
                            - **compiler-sfc:** fix prefixIdentifiers: false handling for inline mode
                            - **reactivity:** fix regression with computed getters tracking
                            - **runtime-core:** handle edge case in slot normalization
                            """,
                        PublishedAt = new DateTime(2024, 11, 15, 0, 0, 0, DateTimeKind.Utc),
                        FetchedAt = now
                    },
                    new Release
                    {
                        Tag = "v3.5.0",
                        Title = "Vue 3.5 \"Tengen Toppa Gurren Lagann\"",
                        Body = """
                            ## Highlights

                            ### Reactive Props Destructure
                            Props destructure is now stable. Variables destructured from `defineProps` are now reactive.

                            ### SSR Improvements
                            - Lazy hydration with `defineAsyncComponent()`
                            - `useId()` for generating unique IDs

                            ### Custom Elements Improvements
                            - `useHost()` for accessing the host element
                            - `useShadowRoot()` for accessing the shadow root
                            """,
                        PublishedAt = new DateTime(2024, 9, 3, 0, 0, 0, DateTimeKind.Utc),
                        FetchedAt = now
                    }
                ]
            },
            new Package
            {
                NpmName = "next",
                GithubOwner = "vercel",
                GithubRepo = "next.js",
                CreatedAt = now,
                LastFetchedAt = now,
                Releases =
                [
                    new Release
                    {
                        Tag = "v15.1.0",
                        Title = "Next.js 15.1",
                        Body = """
                            ## What's Changed

                            ### `after` API (Stable)
                            The `after` API is now stable. Use it to execute code after a response has finished streaming.

                            ### `forbidden` and `unauthorized` APIs
                            New APIs for handling 403 and 401 responses in your application with proper error boundaries.

                            ### Improvements
                            - Improved error overlay with better source maps
                            - React 19 support improvements
                            - Turbopack performance optimizations
                            """,
                        PublishedAt = new DateTime(2024, 12, 10, 0, 0, 0, DateTimeKind.Utc),
                        FetchedAt = now
                    },
                    new Release
                    {
                        Tag = "v15.0.0",
                        Title = "Next.js 15",
                        Body = """
                            ## Next.js 15

                            ### React 19 Support
                            Next.js 15 includes support for React 19, including the new React Compiler.

                            ### Async Request APIs
                            `cookies`, `headers`, and `params` are now async. This is a breaking change.

                            ### Caching Changes
                            - `fetch` requests are no longer cached by default
                            - Route Handlers are no longer cached by default
                            - Client Router Cache no longer caches Page components by default

                            ### Turbopack Dev (Stable)
                            Turbopack is now stable for development with up to 76% faster local server startup.
                            """,
                        PublishedAt = new DateTime(2024, 10, 21, 0, 0, 0, DateTimeKind.Utc),
                        FetchedAt = now
                    }
                ]
            },
            new Package
            {
                NpmName = "typescript",
                GithubOwner = "microsoft",
                GithubRepo = "TypeScript",
                CreatedAt = now,
                LastFetchedAt = now,
                Releases =
                [
                    new Release
                    {
                        Tag = "v5.7.2",
                        Title = "TypeScript 5.7.2",
                        Body = """
                            ## Bug Fixes
                            - Fixed crash when using `satisfies` with certain generic types
                            - Fixed incorrect narrowing with `in` operator
                            - Performance improvements for large union types
                            """,
                        PublishedAt = new DateTime(2024, 12, 3, 0, 0, 0, DateTimeKind.Utc),
                        FetchedAt = now
                    },
                    new Release
                    {
                        Tag = "v5.7.0",
                        Title = "TypeScript 5.7",
                        Body = """
                            ## What's New

                            ### Checked Imports
                            TypeScript now validates that `import` attributes are correct at compile time.

                            ### Path Rewriting for Relative Paths
                            New compiler option `--rewriteRelativeImportExtensions` for rewriting `.ts` to `.js` in output.

                            ### Support for V8 Compile Caching
                            Node.js 22 compile caching is now supported for faster startup times.

                            ### Searching Ancestor Configuration Files
                            `--build` mode now searches for `tsconfig.json` files in ancestor directories.
                            """,
                        PublishedAt = new DateTime(2024, 11, 22, 0, 0, 0, DateTimeKind.Utc),
                        FetchedAt = now
                    }
                ]
            },
            new Package
            {
                NpmName = "tailwindcss",
                GithubOwner = "tailwindlabs",
                GithubRepo = "tailwindcss",
                CreatedAt = now,
                LastFetchedAt = now,
                Releases =
                [
                    new Release
                    {
                        Tag = "v4.0.0",
                        Title = "Tailwind CSS v4.0",
                        Body = """
                            ## Tailwind CSS v4.0

                            A ground-up rewrite with a new high-performance engine, unified toolchain, and CSS-first configuration.

                            ### Highlights
                            - **10x faster** - Full builds complete in milliseconds
                            - **CSS-first configuration** - Configure Tailwind directly in CSS using `@theme`
                            - **Native CSS features** - Built on `@layer`, `@property`, and container queries
                            - **Zero configuration content detection** - Automatic template detection

                            ### Breaking Changes
                            - Configuration is now done in CSS, not JavaScript
                            - Some utility classes have been renamed for consistency
                            - Default color palette changes
                            """,
                        PublishedAt = new DateTime(2025, 1, 15, 0, 0, 0, DateTimeKind.Utc),
                        FetchedAt = now
                    },
                    new Release
                    {
                        Tag = "v3.4.17",
                        Title = "Tailwind CSS v3.4.17",
                        Body = """
                            ## Bug Fixes
                            - Fixed regression with `@apply` in certain edge cases
                            - Fixed color opacity modifier with CSS variables
                            """,
                        PublishedAt = new DateTime(2024, 12, 18, 0, 0, 0, DateTimeKind.Utc),
                        FetchedAt = now
                    }
                ]
            },
            new Package
            {
                NpmName = "vite",
                GithubOwner = "vitejs",
                GithubRepo = "vite",
                CreatedAt = now,
                LastFetchedAt = now,
                Releases =
                [
                    new Release
                    {
                        Tag = "v6.0.7",
                        Title = "Vite 6.0.7",
                        Body = """
                            ## Bug Fixes
                            - fix: handle special characters in module ids during HMR
                            - fix: preserve query parameters in optimized deps
                            - fix(css): handle `@import` with layer correctly
                            """,
                        PublishedAt = new DateTime(2025, 1, 7, 0, 0, 0, DateTimeKind.Utc),
                        FetchedAt = now
                    },
                    new Release
                    {
                        Tag = "v6.0.0",
                        Title = "Vite 6",
                        Body = """
                            ## Vite 6.0

                            ### Environment API
                            New Environment API for better SSR and multi-environment builds.

                            ### Default Value Changes
                            - `resolve.conditions` default value changed
                            - JSON stringify default changed to `'auto'`
                            - `build.cssMinify` defaults to `'esbuild'`

                            ### Performance
                            - Faster dependency optimization
                            - Improved HMR performance
                            - Better tree-shaking
                            """,
                        PublishedAt = new DateTime(2024, 11, 26, 0, 0, 0, DateTimeKind.Utc),
                        FetchedAt = now
                    }
                ]
            },
            new Package
            {
                NpmName = "eslint",
                GithubOwner = "eslint",
                GithubRepo = "eslint",
                CreatedAt = now,
                LastFetchedAt = now,
                Releases =
                [
                    new Release
                    {
                        Tag = "v9.17.0",
                        Title = "ESLint v9.17.0",
                        Body = """
                            ## Features
                            - Add `no-useless-template-literals` rule
                            - Support `ignoreComputedKeys` option in `sort-keys` rule

                            ## Bug Fixes
                            - Fix false positive in `no-unused-vars` with destructuring
                            - Fix `no-restricted-imports` with type imports
                            """,
                        PublishedAt = new DateTime(2024, 12, 13, 0, 0, 0, DateTimeKind.Utc),
                        FetchedAt = now
                    },
                    new Release
                    {
                        Tag = "v9.0.0",
                        Title = "ESLint v9.0.0",
                        Body = """
                            ## ESLint v9.0.0

                            ### Flat Config by Default
                            The new flat config system is now the default. Legacy `.eslintrc` files are no longer supported by default.

                            ### Breaking Changes
                            - Node.js 18.18.0+ required
                            - `eslint:recommended` updated with new rules
                            - Removed deprecated rules and formatters

                            ### New Rules
                            - `no-new-native-nonconstructor`
                            - `no-constant-binary-expression`
                            """,
                        PublishedAt = new DateTime(2024, 4, 5, 0, 0, 0, DateTimeKind.Utc),
                        FetchedAt = now
                    }
                ]
            },
            new Package
            {
                NpmName = "zod",
                GithubOwner = "colinhacks",
                GithubRepo = "zod",
                CreatedAt = now,
                LastFetchedAt = now,
                Releases =
                [
                    new Release
                    {
                        Tag = "v3.24.1",
                        Title = "Zod 3.24.1",
                        Body = """
                            ## Bug Fixes
                            - Fixed `z.function()` parameter inference with transforms
                            - Fixed `z.discriminatedUnion()` error messages
                            - Performance improvements for large schemas
                            """,
                        PublishedAt = new DateTime(2024, 12, 20, 0, 0, 0, DateTimeKind.Utc),
                        FetchedAt = now
                    },
                    new Release
                    {
                        Tag = "v3.24.0",
                        Title = "Zod 3.24",
                        Body = """
                            ## New Features

                            ### File Schema
                            New `z.file()` schema for validating `File` objects in the browser.

                            ### Number Coercion Options
                            `z.coerce.number()` now accepts options for controlling coercion behavior.

                            ### Improved Error Messages
                            Better default error messages for common validation failures.
                            """,
                        PublishedAt = new DateTime(2024, 12, 5, 0, 0, 0, DateTimeKind.Utc),
                        FetchedAt = now
                    }
                ]
            }
        ];
    }
}
