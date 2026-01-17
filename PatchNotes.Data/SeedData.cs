using Microsoft.EntityFrameworkCore;

namespace PatchNotes.Data;

public static class SeedData
{
    public static async Task SeedAsync(PatchNotesDbContext context)
    {
        if (await context.Packages.AnyAsync())
        {
            return; // Already seeded
        }

        var packages = GetSamplePackages();
        context.Packages.AddRange(packages);
        await context.SaveChangesAsync();
    }

    public static List<Package> GetSamplePackages()
    {
        var now = DateTime.UtcNow;

        return
        [
            new Package
            {
                NpmName = "react",
                GithubOwner = "facebook",
                GithubRepo = "react",
                LastFetchedAt = now,
                CreatedAt = now.AddDays(-30),
                Releases =
                [
                    new Release
                    {
                        Tag = "v19.0.0",
                        Title = "React 19",
                        Body = """
                            ## What's New

                            React 19 is now stable! This release includes several new features:

                            ### Actions
                            Actions allow you to pass async functions to `useTransition` and `<form>` elements.

                            ### New Hooks
                            - `useActionState` - Manage state for form actions
                            - `useFormStatus` - Access form submission status
                            - `useOptimistic` - Optimistic UI updates

                            ### Server Components
                            Server Components are now stable and ready for production use.

                            ### Document Metadata
                            Built-in support for `<title>`, `<meta>`, and `<link>` tags.

                            See the [upgrade guide](https://react.dev/blog/2024/12/05/react-19) for migration instructions.
                            """,
                        PublishedAt = now.AddDays(-7),
                        FetchedAt = now
                    },
                    new Release
                    {
                        Tag = "v18.3.1",
                        Title = "React 18.3.1",
                        Body = """
                            ## Bug Fixes

                            - Fixed a regression in `useSyncExternalStore` that could cause unnecessary re-renders
                            - Improved error messages for invalid hook usage
                            - Fixed memory leak in concurrent rendering mode
                            """,
                        PublishedAt = now.AddDays(-45),
                        FetchedAt = now
                    },
                    new Release
                    {
                        Tag = "v18.3.0",
                        Title = "React 18.3.0",
                        Body = """
                            ## Deprecation Warnings

                            This release adds deprecation warnings for APIs that will be removed or changed in React 19:

                            - `defaultProps` on function components
                            - String refs
                            - Legacy Context (`contextTypes` and `getChildContext`)

                            These warnings help you prepare your codebase for the React 19 upgrade.
                            """,
                        PublishedAt = now.AddDays(-90),
                        FetchedAt = now
                    }
                ]
            },
            new Package
            {
                NpmName = "typescript",
                GithubOwner = "microsoft",
                GithubRepo = "TypeScript",
                LastFetchedAt = now,
                CreatedAt = now.AddDays(-30),
                Releases =
                [
                    new Release
                    {
                        Tag = "v5.7.2",
                        Title = "TypeScript 5.7.2",
                        Body = """
                            ## Bug Fixes

                            - Fixed crash when using `satisfies` with certain conditional types
                            - Improved performance of type inference for large unions
                            - Fixed incorrect narrowing in `switch` statements with `default` clause
                            """,
                        PublishedAt = now.AddDays(-3),
                        FetchedAt = now
                    },
                    new Release
                    {
                        Tag = "v5.7.0",
                        Title = "TypeScript 5.7",
                        Body = """
                            ## What's New

                            ### Checks for Never-Initialized Variables
                            TypeScript now errors when variables are used before being assigned in all code paths.

                            ### Path Rewriting for Relative Paths
                            New `--rewriteRelativeImportExtensions` flag for ESM compatibility.

                            ### Support for `--target es2024`
                            Enables `Object.groupBy`, `Map.groupBy`, and `Promise.withResolvers`.

                            ### Faster Project Builds
                            Significant performance improvements for incremental builds.

                            See the [release notes](https://devblogs.microsoft.com/typescript/announcing-typescript-5-7/) for details.
                            """,
                        PublishedAt = now.AddDays(-14),
                        FetchedAt = now
                    }
                ]
            },
            new Package
            {
                NpmName = "next",
                GithubOwner = "vercel",
                GithubRepo = "next.js",
                LastFetchedAt = now,
                CreatedAt = now.AddDays(-30),
                Releases =
                [
                    new Release
                    {
                        Tag = "v15.1.0",
                        Title = "Next.js 15.1",
                        Body = """
                            ## What's New

                            ### `after` API (Stable)
                            Execute code after the response has finished streaming.

                            ### `forbidden` and `unauthorized` APIs
                            New APIs for handling 403 and 401 responses in App Router.

                            ### Improved Static Generation
                            Better handling of dynamic routes with `generateStaticParams`.

                            ## Bug Fixes
                            - Fixed hydration mismatch warnings in development
                            - Improved error overlay accessibility
                            - Fixed memory leak in development server
                            """,
                        PublishedAt = now.AddDays(-5),
                        FetchedAt = now
                    },
                    new Release
                    {
                        Tag = "v15.0.4",
                        Title = "Next.js 15.0.4",
                        Body = """
                            ## Bug Fixes

                            - Fixed issue with parallel routes not rendering correctly
                            - Improved TypeScript support for Server Actions
                            - Fixed `next/image` optimization for animated images
                            - Resolved build errors with certain Webpack configurations
                            """,
                        PublishedAt = now.AddDays(-21),
                        FetchedAt = now
                    }
                ]
            },
            new Package
            {
                NpmName = "tailwindcss",
                GithubOwner = "tailwindlabs",
                GithubRepo = "tailwindcss",
                LastFetchedAt = now,
                CreatedAt = now.AddDays(-30),
                Releases =
                [
                    new Release
                    {
                        Tag = "v4.0.0",
                        Title = "Tailwind CSS v4.0",
                        Body = """
                            ## A new engine, built for speed

                            Tailwind CSS v4.0 is a ground-up rewrite with a new high-performance engine.

                            ### Highlights

                            - **10x faster** - Full builds complete in milliseconds
                            - **Unified toolchain** - Built-in CSS processing, no PostCSS required
                            - **CSS-first configuration** - Configure directly in CSS with `@theme`
                            - **Modern CSS features** - Native cascade layers, `color-mix()`, and more
                            - **Simplified installation** - Single dependency, zero configuration

                            ### Breaking Changes

                            - Requires Node.js 20+
                            - New `@import "tailwindcss"` syntax
                            - Configuration moved to CSS `@theme` directive

                            See the [upgrade guide](https://tailwindcss.com/docs/upgrade-guide) for migration steps.
                            """,
                        PublishedAt = now.AddDays(-10),
                        FetchedAt = now
                    },
                    new Release
                    {
                        Tag = "v3.4.17",
                        Title = "Tailwind CSS v3.4.17",
                        Body = """
                            ## Bug Fixes

                            - Fixed specificity issues with arbitrary variants
                            - Improved JIT compiler stability
                            - Fixed `@apply` not working with certain plugin utilities
                            """,
                        PublishedAt = now.AddDays(-60),
                        FetchedAt = now
                    }
                ]
            },
            new Package
            {
                NpmName = "vite",
                GithubOwner = "vitejs",
                GithubRepo = "vite",
                LastFetchedAt = now,
                CreatedAt = now.AddDays(-30),
                Releases =
                [
                    new Release
                    {
                        Tag = "v6.0.7",
                        Title = "Vite 6.0.7",
                        Body = """
                            ## Bug Fixes

                            - fix: handle circular dependencies in optimized deps
                            - fix: improve HMR reliability for CSS modules
                            - fix: resolve aliases in worker scripts correctly
                            - perf: reduce memory usage during dependency optimization
                            """,
                        PublishedAt = now.AddDays(-2),
                        FetchedAt = now
                    },
                    new Release
                    {
                        Tag = "v6.0.0",
                        Title = "Vite 6",
                        Body = """
                            ## Vite 6 is out!

                            ### Environment API
                            New unified API for configuring different environments (client, SSR, workerd, etc.)

                            ### New Defaults
                            - `build.modulePreload` now defaults to `false`
                            - `resolve.conditions` updated for better ESM compatibility

                            ### Performance
                            - Faster dependency pre-bundling with improved caching
                            - Reduced memory footprint in development

                            ### Breaking Changes
                            - Requires Node.js 20+
                            - Vite Runtime API changes

                            See [migration guide](https://vite.dev/guide/migration.html) for details.
                            """,
                        PublishedAt = now.AddDays(-30),
                        FetchedAt = now
                    }
                ]
            }
        ];
    }
}
