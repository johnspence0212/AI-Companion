import { beforeEach, describe, expect, it, vi } from 'vitest'
import { clearCsrfToken, httpClient } from '../client'

describe('httpClient antiforgery handling', () => {
  beforeEach(() => {
    clearCsrfToken()
    vi.restoreAllMocks()
  })

  it('fetches and sends a request token for unsafe requests', async () => {
    const fetchMock = vi
      .spyOn(globalThis, 'fetch')
      .mockResolvedValueOnce(
        new Response(JSON.stringify({ requestToken: 'csrf-token' }), {
          status: 200,
          headers: { 'Content-Type': 'application/json' },
        }),
      )
      .mockResolvedValueOnce(new Response(null, { status: 204 }))

    await httpClient.post<void>('/example', { enabled: true })

    expect(fetchMock).toHaveBeenCalledTimes(2)
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/auth/csrf')
    const mutationOptions = fetchMock.mock.calls[1]?.[1]
    expect(new Headers(mutationOptions?.headers).get('X-CSRF-TOKEN')).toBe('csrf-token')
    expect(mutationOptions?.credentials).toBe('include')
  })
})
