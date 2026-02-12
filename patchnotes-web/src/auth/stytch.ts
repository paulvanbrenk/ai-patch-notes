import { StytchUIClient } from '@stytch/vanilla-js'
import { Products } from '@stytch/vanilla-js'

const publicToken = import.meta.env.VITE_STYTCH_PUBLIC_TOKEN

if (!publicToken) {
  console.warn(
    'VITE_STYTCH_PUBLIC_TOKEN is not set. Authentication will not work.'
  )
}

export const stytchClient = new StytchUIClient(publicToken || '', {
  cookieOptions: {
    // Share session cookies across *.myreleasenotes.ai so the API
    // subdomain (api.myreleasenotes.ai) receives them on requests.
    ...(import.meta.env.PROD && {
      domain: 'myreleasenotes.ai',
      availableToSubdomains: true,
    }),
  },
})

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
  // Use transparent container to blend with page background
  // Style inputs with borders instead of background contrast
  const colors = isDark
    ? {
        surface: 'transparent',
        text: '#e5e7eb',
        textSecondary: '#9ca3af',
        border: '#374151',
        inputBg: 'rgba(255, 255, 255, 0.05)',
        brand: '#818cf8',
      }
    : {
        surface: 'transparent',
        text: '#1f2937',
        textSecondary: '#6b7280',
        border: '#d1d5db',
        inputBg: '#ffffff',
        brand: '#4f46e5',
      }

  return {
    fontFamily: '"Inter", ui-sans-serif, system-ui, sans-serif',
    container: {
      backgroundColor: colors.surface,
      borderColor: colors.border,
      borderRadius: '12px',
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
      backgroundColor: colors.inputBg,
      borderColor: colors.border,
      borderRadius: '8px',
      textColor: colors.text,
      placeholderColor: colors.textSecondary,
    },
  }
}
