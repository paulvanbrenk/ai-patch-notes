import { StytchUIClient } from '@stytch/vanilla-js'
import { Products } from '@stytch/vanilla-js'

const publicToken = import.meta.env.VITE_STYTCH_PUBLIC_TOKEN

if (!publicToken) {
  console.warn(
    'VITE_STYTCH_PUBLIC_TOKEN is not set. Authentication will not work.'
  )
}

export const stytchClient = new StytchUIClient(publicToken || '')

export const stytchLoginConfig = {
  products: [Products.emailMagicLinks, Products.oauth],
  emailMagicLinksOptions: {
    loginRedirectURL: `${window.location.origin}/authenticate`,
    loginExpirationMinutes: 30,
    signupRedirectURL: `${window.location.origin}/authenticate`,
    signupExpirationMinutes: 30,
  },
  oauthOptions: {
    providers: [{ type: 'google' as const }],
    loginRedirectURL: `${window.location.origin}/authenticate`,
    signupRedirectURL: `${window.location.origin}/authenticate`,
  },
}
