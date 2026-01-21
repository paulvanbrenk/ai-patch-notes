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
