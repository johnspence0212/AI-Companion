import { describe, expect, it } from 'vitest'
import { ApiError } from '@/api/base/client'
import { parseManagedUser, parseUser } from '@/api/types/schema'

describe('parseUser', () => {
  it('normalizes optional fields and permission arrays', () => {
    const user = parseUser({
      id: '1',
      email: 'user@example.com',
      displayName: 42,
      isActive: false,
      roles: ['Member', 1],
      permissions: ['users.read', null],
      mustChangePassword: 'yes',
      createdAt: 123,
      lastLoginAt: '2026-02-01T00:00:00Z',
    })

    expect(user).toEqual({
      id: '1',
      email: 'user@example.com',
      displayName: null,
      isActive: false,
      roles: ['Member'],
      permissions: ['users.read'],
      mustChangePassword: false,
      createdAt: '',
      lastLoginAt: '2026-02-01T00:00:00Z',
    })
  })

  it('rejects invalid payloads', () => {
    expect(() => parseUser({ email: 'missing-id' })).toThrow(ApiError)
  })
})

describe('parseManagedUser', () => {
  it('treats inactive users as disabled', () => {
    const user = parseManagedUser({
      id: '2',
      email: 'managed@example.com',
      roles: ['Member'],
      isActive: false,
      mustChangePassword: true,
    })

    expect(user.isDisabled).toBe(true)
    expect(user.mustChangePassword).toBe(true)
  })
})
