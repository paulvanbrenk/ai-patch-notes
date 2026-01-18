import type { FallbackProps } from 'react-error-boundary'

export function ErrorFallback({ error, resetErrorBoundary }: FallbackProps) {
  const errorMessage =
    error instanceof Error ? error.message : 'An unknown error occurred'

  return (
    <div className="flex min-h-screen items-center justify-center bg-gray-50 p-4">
      <div className="max-w-md rounded-lg bg-white p-6 shadow-lg">
        <h1 className="mb-4 text-xl font-semibold text-gray-900">
          Something went wrong
        </h1>
        <p className="mb-4 text-gray-600">
          An unexpected error occurred. Please try again.
        </p>
        <details className="mb-4">
          <summary className="cursor-pointer text-sm text-gray-500">
            Error details
          </summary>
          <pre className="mt-2 overflow-auto rounded bg-gray-100 p-2 text-xs text-red-600">
            {errorMessage}
          </pre>
        </details>
        <button
          onClick={resetErrorBoundary}
          className="rounded bg-blue-600 px-4 py-2 text-white hover:bg-blue-700"
        >
          Try again
        </button>
      </div>
    </div>
  )
}
