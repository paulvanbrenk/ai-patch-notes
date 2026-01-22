import { useEffect } from 'react'
import { useThemeStore, getSystemTheme, applyTheme } from './themeStore'

// Simple provider that just sets up the system listener
export function ThemeProvider({ children }: { children: React.ReactNode }) {
  // Initialize theme on mount
  useEffect(() => {
    const state = useThemeStore.getState()
    const resolved = state.theme === 'system' ? getSystemTheme() : state.theme
    applyTheme(resolved)
    if (state.resolvedTheme !== resolved) {
      state.setResolvedTheme(resolved)
    }
  }, [])

  return <>{children}</>
}
