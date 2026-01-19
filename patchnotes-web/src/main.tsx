import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { ErrorBoundary } from 'react-error-boundary'
import { RouterProvider } from '@tanstack/react-router'
import { QueryClientProvider } from '@tanstack/react-query'
import { router } from './router'
import { queryClient } from './queryClient'
import { ErrorFallback } from './components/ErrorFallback'
import { ToastProvider } from './components/Toast'
import { QueryErrorHandler } from './components/QueryErrorHandler'
import './index.css'

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <ErrorBoundary FallbackComponent={ErrorFallback}>
      <ToastProvider>
        <QueryClientProvider client={queryClient}>
          <QueryErrorHandler />
          <RouterProvider router={router} />
        </QueryClientProvider>
      </ToastProvider>
    </ErrorBoundary>
  </StrictMode>
)
