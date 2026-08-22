import { describe, expect, it } from 'vitest'
import { isApiRequestPath } from '../pwaPolicy'

describe('isApiRequestPath', () => {
  it('treats versioned and unversioned API paths as network-only', () => {
    expect(isApiRequestPath('/api')).toBe(true)
    expect(isApiRequestPath('/api/v1/auth/me')).toBe(true)
    expect(isApiRequestPath('/api/v1/auth/csrf')).toBe(true)
  })

  it('does not treat application routes as API', () => {
    expect(isApiRequestPath('/')).toBe(false)
    expect(isApiRequestPath('/login')).toBe(false)
    expect(isApiRequestPath('/admin/users')).toBe(false)
  })
})
