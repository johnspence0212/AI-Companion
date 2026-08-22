/**
 * Production PWA policy for the same SPA.
 * Dev (`npm run dev` / Aspire) does not register a service worker.
 * `/api` is never intercepted as a navigation fallback — cookie + CSRF always hit the network.
 */
export const pwaThemeColor = '#1b4332'
export const pwaBackgroundColor = '#ffffff'

export function isApiRequestPath(pathname: string): boolean {
  return pathname === '/api' || pathname.startsWith('/api/')
}
