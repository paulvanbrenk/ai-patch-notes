import { useEffect } from 'react'
import { useToast } from './Toast'
import { setGlobalErrorHandler } from '../queryClient'

export function QueryErrorHandler() {
  const { showError } = useToast()

  useEffect(() => {
    setGlobalErrorHandler(showError)
  }, [showError])

  return null
}
