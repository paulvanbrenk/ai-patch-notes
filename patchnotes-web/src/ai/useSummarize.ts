import { useState, useCallback, useRef } from 'react'

const API_BASE_URL = import.meta.env.VITE_API_URL || '/api'

export interface SummarizeResult {
  releaseId: string
  tag: string
  title: string | null
  summary: string
  package: {
    id: string
    npmName: string
  }
}

export interface UseSummarizeOptions {
  onSuccess?: (result: SummarizeResult) => void
  onError?: (error: Error) => void
}

export function useSummarize(options: UseSummarizeOptions = {}) {
  const [isLoading, setIsLoading] = useState(false)
  const [error, setError] = useState<Error | null>(null)
  const [summary, setSummary] = useState<string | null>(null)
  const optionsRef = useRef(options)
  optionsRef.current = options

  const summarize = useCallback(
    async (releaseId: string) => {
      setIsLoading(true)
      setError(null)
      setSummary(null)

      try {
        const response = await fetch(
          `${API_BASE_URL}/releases/${releaseId}/summarize`,
          {
            method: 'POST',
            headers: {
              Accept: 'text/event-stream',
            },
            credentials: 'include',
          }
        )

        if (!response.ok) {
          throw new Error(`Failed to summarize: ${response.statusText}`)
        }

        const contentType = response.headers.get('content-type')

        // Handle SSE streaming response
        if (contentType?.includes('text/event-stream')) {
          const reader = response.body?.getReader()
          if (!reader) {
            throw new Error('No response body')
          }

          const decoder = new TextDecoder()
          let fullSummary = ''
          let result: SummarizeResult | null = null

          while (true) {
            const { done, value } = await reader.read()
            if (done) break

            const chunk = decoder.decode(value, { stream: true })
            const lines = chunk.split('\n')

            for (const line of lines) {
              if (line.startsWith('data: ')) {
                const data = line.slice(6)
                if (data === '[DONE]') continue

                try {
                  const parsed = JSON.parse(data)
                  if (parsed.type === 'chunk') {
                    fullSummary += parsed.content
                    setSummary(fullSummary)
                  } else if (parsed.type === 'complete') {
                    result = parsed.result
                  }
                } catch {
                  // Ignore parse errors for partial data
                }
              }
            }
          }

          if (result) {
            optionsRef.current.onSuccess?.(result)
          }
        } else {
          // Handle JSON response (non-streaming fallback)
          const data = (await response.json()) as SummarizeResult
          setSummary(data.summary)
          optionsRef.current.onSuccess?.(data)
        }
      } catch (err) {
        const error = err instanceof Error ? err : new Error('Unknown error')
        setError(error)
        optionsRef.current.onError?.(error)
      } finally {
        setIsLoading(false)
      }
    },
    []
  )

  const reset = useCallback(() => {
    setSummary(null)
    setError(null)
  }, [])

  return {
    summarize,
    isLoading,
    error,
    summary,
    reset,
  }
}
