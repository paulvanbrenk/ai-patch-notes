import { useState, useCallback, useRef, useEffect } from 'react'

import { API_ROOT } from '../api/client'

const API_BASE_URL = `${API_ROOT}/api`

interface SummarizeResult {
  releaseId: string
  tag: string
  title: string | null
  summary: string
  package: {
    id: string
    npmName: string
  }
}

interface UseSummarizeOptions {
  onSuccess?: (result: SummarizeResult) => void
  onError?: (error: Error) => void
}

async function fetchSummary(
  releaseId: string,
  signal: AbortSignal,
  onChunk: (fullSummary: string) => void
): Promise<SummarizeResult> {
  const response = await fetch(
    `${API_BASE_URL}/releases/${releaseId}/summarize`,
    {
      method: 'POST',
      headers: {
        Accept: 'text/event-stream',
      },
      credentials: 'include',
      signal,
    }
  )

  if (!response.ok) {
    return Promise.reject(
      new Error(`Failed to summarize: ${response.statusText}`)
    )
  }

  const contentType = response.headers.get('content-type')

  // Handle JSON response (non-streaming fallback)
  if (!contentType?.includes('text/event-stream')) {
    const data = (await response.json()) as SummarizeResult
    onChunk(data.summary)
    return data
  }

  // Handle SSE streaming response
  const reader = response.body?.getReader()
  if (!reader) {
    return Promise.reject(new Error('No response body'))
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
            onChunk(fullSummary)
          } else if (parsed.type === 'complete') {
            result = parsed.result
          }
        } catch {
          // Ignore parse errors for partial data
        }
      }
    }
  }

  if (!result) {
    return Promise.reject(new Error('No result received from stream'))
  }

  return result
}

export function useSummarize(options: UseSummarizeOptions = {}) {
  const [isLoading, setIsLoading] = useState(false)
  const [error, setError] = useState<Error | null>(null)
  const [summary, setSummary] = useState<string | null>(null)
  const optionsRef = useRef(options)
  useEffect(() => {
    optionsRef.current = options
  })
  const abortControllerRef = useRef<AbortController | null>(null)

  const summarize = useCallback(async (releaseId: string) => {
    // Abort any in-flight request before starting a new one
    abortControllerRef.current?.abort()
    const controller = new AbortController()
    abortControllerRef.current = controller

    setIsLoading(true)
    setError(null)
    setSummary(null)

    await fetchSummary(releaseId, controller.signal, setSummary).then(
      (result) => {
        if (!controller.signal.aborted) {
          optionsRef.current.onSuccess?.(result)
          setIsLoading(false)
        }
      },
      (err) => {
        if (controller.signal.aborted) return

        const error = err instanceof Error ? err : new Error('Unknown error')
        setError(error)
        optionsRef.current.onError?.(error)
        setIsLoading(false)
      }
    )
  }, [])

  const cancel = useCallback(() => {
    abortControllerRef.current?.abort()
    abortControllerRef.current = null
    setIsLoading(false)
  }, [])

  const reset = useCallback(() => {
    cancel()
    setSummary(null)
    setError(null)
  }, [cancel])

  return {
    summarize,
    isLoading,
    error,
    summary,
    cancel,
    reset,
  }
}
