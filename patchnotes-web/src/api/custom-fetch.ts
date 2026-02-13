import { ApiError } from './client.ts'

const API_BASE_URL = import.meta.env.VITE_API_URL || '/api'

/**
 * Custom fetch instance for Orval-generated API hooks.
 * Orval calls this as customFetch<T>(url, requestInit).
 */
export const customFetch = async <T>(
  url: string,
  init: RequestInit
): Promise<T> => {
  const fullUrl = `${API_BASE_URL}${url}`

  const config: RequestInit = {
    ...init,
    credentials: 'include',
    headers: {
      'Content-Type': 'application/json',
      ...init.headers,
    },
  }

  let response: Response
  try {
    response = await fetch(fullUrl, config)
  } catch (error) {
    throw new ApiError(
      0,
      error instanceof Error ? error.message : 'Network request failed',
      undefined,
      true
    )
  }

  if (!response.ok) {
    const errorData = await response.json().catch(() => null)
    throw new ApiError(response.status, response.statusText, errorData)
  }

  if (response.status === 204) {
    return undefined as T
  }

  return response.json()
}

export default customFetch
