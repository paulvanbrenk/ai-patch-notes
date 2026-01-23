import { create } from 'zustand'
import {
  getSubscriptionStatus,
  createCheckoutSession,
  createPortalSession,
} from '../api/subscription'

interface SubscriptionState {
  isPro: boolean
  status: string | null
  expiresAt: string | null
  isLoading: boolean
  error: string | null
  checkSubscription: () => Promise<void>
  startCheckout: () => Promise<void>
  openPortal: () => Promise<void>
  reset: () => void
}

export const useSubscriptionStore = create<SubscriptionState>((set) => ({
  isPro: false,
  status: null,
  expiresAt: null,
  isLoading: false,
  error: null,

  checkSubscription: async () => {
    set({ isLoading: true, error: null })
    try {
      const { isPro, status, expiresAt } = await getSubscriptionStatus()
      set({ isPro, status, expiresAt, isLoading: false })
    } catch (error) {
      // If unauthorized, user is not logged in - not an error state
      if (error instanceof Error && error.message.includes('401')) {
        set({ isPro: false, status: null, expiresAt: null, isLoading: false })
      } else {
        set({
          error:
            error instanceof Error
              ? error.message
              : 'Failed to check subscription',
          isLoading: false,
        })
      }
    }
  },

  startCheckout: async () => {
    set({ isLoading: true, error: null })
    try {
      const { url } = await createCheckoutSession()
      // Redirect to Stripe Checkout
      window.location.href = url
    } catch (error) {
      set({
        error:
          error instanceof Error ? error.message : 'Failed to start checkout',
        isLoading: false,
      })
    }
  },

  openPortal: async () => {
    set({ isLoading: true, error: null })
    try {
      const { url } = await createPortalSession()
      // Redirect to Stripe Customer Portal
      window.location.href = url
    } catch (error) {
      set({
        error: error instanceof Error ? error.message : 'Failed to open portal',
        isLoading: false,
      })
    }
  },

  reset: () => {
    set({
      isPro: false,
      status: null,
      expiresAt: null,
      isLoading: false,
      error: null,
    })
  },
}))
