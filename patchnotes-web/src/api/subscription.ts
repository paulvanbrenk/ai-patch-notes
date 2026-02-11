import { api } from './client'

export interface SubscriptionStatus {
  isPro: boolean
  status: string | null
  expiresAt: string | null
}

export async function getSubscriptionStatus(): Promise<SubscriptionStatus> {
  return api.get<SubscriptionStatus>('/subscription/status')
}
