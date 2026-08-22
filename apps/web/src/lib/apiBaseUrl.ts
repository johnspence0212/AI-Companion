/** Resolves the versioned API base URL used by every frontend client. */
export function resolveApiBaseUrl(): string {
  const raw = import.meta.env.VITE_API_BASE_URL
  if (typeof raw === 'string' && raw.trim() !== '') {
    return normalizeApiBaseUrl(raw.trim())
  }

  // Local `npm run dev` without Aspire: Vite proxies /api → localhost:5000.
  if (import.meta.env.DEV) {
    return '/api/v1'
  }

  return '/api/v1'
}

export function normalizeApiBaseUrl(url: string): string {
  const withoutTrailingSlash = url.replace(/\/+$/, '')
  if (withoutTrailingSlash.endsWith('/api/v1')) {
    return withoutTrailingSlash
  }
  if (withoutTrailingSlash.endsWith('/api')) {
    return `${withoutTrailingSlash}/v1`
  }
  return `${withoutTrailingSlash}/api/v1`
}
