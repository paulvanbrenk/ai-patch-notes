import { StytchUIClient } from '@stytch/vanilla-js'
import { Products } from '@stytch/vanilla-js'

const publicToken = import.meta.env.VITE_STYTCH_PUBLIC_TOKEN

if (!publicToken) {
  console.warn(
    'VITE_STYTCH_PUBLIC_TOKEN is not set. Authentication will not work.'
  )
}

export const stytchClient = new StytchUIClient(publicToken || '')

const products = [Products.emailMagicLinks]

if (import.meta.env.DEV) {
  products.push(Products.passwords)
}

export const stytchLoginConfig = {
  products,
  emailMagicLinksOptions: {
    loginRedirectURL: `${window.location.origin}/authenticate`,
    loginExpirationMinutes: 30,
    signupRedirectURL: `${window.location.origin}/authenticate`,
    signupExpirationMinutes: 30,
  },
  ...(import.meta.env.DEV && {
    passwordOptions: {
      loginRedirectURL: `${window.location.origin}/authenticate`,
      resetPasswordRedirectURL: `${window.location.origin}/authenticate`,
    },
  }),
}

// Theme-aware styles for Stytch login component
export function getStytchStyles(isDark: boolean) {
  const colors = isDark
    ? {
        surface: '#1f2937',
        surfaceSecondary: '#111827',
        text: '#f3f4f6',
        textSecondary: '#9ca3af',
        border: '#374151',
        brand: '#6366f1',
        brandHover: '#4f46e5',
      }
    : {
        surface: '#ffffff',
        surfaceSecondary: '#f9fafb',
        text: '#1f2937',
        textSecondary: '#6b7280',
        border: '#e5e7eb',
        brand: '#4f46e5',
        brandHover: '#4338ca',
      }

  return {
    fontFamily: '"Inter", ui-sans-serif, system-ui, sans-serif',
    hideHeaderText: false,
    container: {
      backgroundColor: colors.surface,
      borderColor: colors.border,
      borderRadius: '12px',
      width: '100%',
    },
    colors: {
      primary: colors.text,
      secondary: colors.textSecondary,
      success: '#10b981',
      error: '#ef4444',
    },
    buttons: {
      primary: {
        backgroundColor: colors.brand,
        borderColor: colors.brand,
        borderRadius: '8px',
        textColor: '#ffffff',
      },
      secondary: {
        backgroundColor: 'transparent',
        borderColor: colors.border,
        borderRadius: '8px',
        textColor: colors.text,
      },
    },
    inputs: {
      backgroundColor: colors.surfaceSecondary,
      borderColor: colors.border,
      borderRadius: '8px',
      textColor: colors.text,
      placeholderColor: colors.textSecondary,
    },
  }
}
