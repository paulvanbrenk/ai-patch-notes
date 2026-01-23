import { api } from './client'

export interface SubscriptionStatus {
  isPro: boolean
  status: string | null
  expiresAt: string | null
}

export interface CheckoutResponse {
  url: string
}

export interface PortalResponse {
  url: string
}

export async function createCheckoutSession(): Promise<CheckoutResponse> {
  return api.post<CheckoutResponse>('/subscription/checkout')
}

export async function createPortalSession(): Promise<PortalResponse> {
  return api.post<PortalResponse>('/subscription/portal')
}

export async function getSubscriptionStatus(): Promise<SubscriptionStatus> {
  return api.get<SubscriptionStatus>('/subscription/status')
}
