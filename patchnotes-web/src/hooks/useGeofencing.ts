import { useQuery } from '@tanstack/react-query';

export interface GeolocationData {
  country_code: string;
  country_name: string;
  region?: string;
  city?: string;
  ip?: string;
}

export interface GeofencingState {
  isLoading: boolean;
  isAllowed: boolean | null;
  error: string | null;
  data: GeolocationData | null;
}

const ALLOWED_COUNTRIES = ['US', 'CA'];

async function fetchGeolocation(): Promise<GeolocationData> {
  if (import.meta.env.DEV && import.meta.env.VITE_BYPASS_GEOFENCING === 'true') {
    return { country_code: 'US', country_name: 'United States (Development)' };
  }

  const controller = new AbortController();
  const timeoutId = setTimeout(() => controller.abort(), 10000);
  try {
    const response = await fetch('https://ipapi.co/json/', { signal: controller.signal });
    clearTimeout(timeoutId);
    if (!response.ok) throw new Error(`HTTP ${response.status}: ${response.statusText}`);
    const data: GeolocationData = await response.json();
    return data;
  } catch (error) {
    clearTimeout(timeoutId);
    throw error;
  }
}

export function useGeofencing(): GeofencingState {
  const { data, isLoading, error } = useQuery({
    queryKey: ['geolocation'],
    queryFn: fetchGeolocation,
    staleTime: 24 * 60 * 60 * 1000,
    gcTime: 24 * 60 * 60 * 1000,
    retry: (failureCount, error) => {
      if (error instanceof Error && error.name === 'AbortError') return false;
      return failureCount < 2;
    },
    retryDelay: (attemptIndex) => Math.min(1000 * 2 ** attemptIndex, 30000),
  });

  return {
    isLoading,
    isAllowed: error ? true : data ? ALLOWED_COUNTRIES.includes(data.country_code) : null,
    error: error?.message || null,
    data: data || null,
  };
}
