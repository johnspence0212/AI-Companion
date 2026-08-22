import { beforeEach, describe, expect, it, vi } from 'vitest'
import { createPinia, setActivePinia } from 'pinia'
import { ApiError } from '@/api/base/client'
import { useAuthStore } from '@/stores/auth'

vi.mock('@/api/authApi', () => ({
  authApi: {
    login: vi.fn(),
    me: vi.fn(),
    logout: vi.fn(),
    changePassword: vi.fn(),
    updateProfile: vi.fn(),
  },
}))

import { authApi } from '@/api/authApi'

const sampleUser = {
  id: 'user-1',
  email: 'admin@enterprisestarter.local',
  displayName: 'Admin',
  isActive: true,
  roles: ['Admin'],
  permissions: ['users.read', 'roles.read'],
  mustChangePassword: false,
  createdAt: '2026-01-01T00:00:00Z',
  lastLoginAt: null,
}

describe('useAuthStore', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    vi.clearAllMocks()
  })

  it('logs in and exposes permissions', async () => {
    vi.mocked(authApi.login).mockResolvedValue(sampleUser)
    const auth = useAuthStore()

    await auth.login('admin@enterprisestarter.local', 'password')

    expect(auth.isAuthenticated).toBe(true)
    expect(auth.roles).toEqual(['Admin'])
    expect(auth.hasPermission('users.read')).toBe(true)
    expect(auth.hasPermission('audit.read')).toBe(false)
    expect(auth.error).toBeNull()
    expect(auth.loading).toBe(false)
  })

  it('maps locked and rate-limited login failures', async () => {
    const auth = useAuthStore()

    vi.mocked(authApi.login).mockRejectedValueOnce(new ApiError('Locked', 423))
    await expect(auth.login('a@b.c', 'x')).rejects.toBeInstanceOf(ApiError)
    expect(auth.error).toBe('Account is locked. Try again later.')

    vi.mocked(authApi.login).mockRejectedValueOnce(new ApiError('Slow down', 429))
    await expect(auth.login('a@b.c', 'x')).rejects.toBeInstanceOf(ApiError)
    expect(auth.error).toBe('Too many attempts. Wait a moment and try again.')
  })

  it('hydrates from /auth/me and clears session on failure', async () => {
    vi.mocked(authApi.me).mockResolvedValueOnce(sampleUser)
    const auth = useAuthStore()

    await auth.hydrate()
    expect(auth.hydrated).toBe(true)
    expect(auth.user?.email).toBe(sampleUser.email)

    vi.mocked(authApi.me).mockRejectedValueOnce(new Error('unauthorized'))
    auth.hydrated = false
    await auth.hydrate()
    expect(auth.user).toBeNull()
    expect(auth.hydrated).toBe(true)
  })

  it('forces password-change state from the user payload', async () => {
    vi.mocked(authApi.login).mockResolvedValue({
      ...sampleUser,
      mustChangePassword: true,
    })
    const auth = useAuthStore()

    await auth.login('admin@enterprisestarter.local', 'temp')
    expect(auth.mustChangePassword).toBe(true)
  })

  it('clears local session on logout even if the API fails', async () => {
    vi.mocked(authApi.login).mockResolvedValue(sampleUser)
    vi.mocked(authApi.logout).mockRejectedValue(new Error('offline'))
    const auth = useAuthStore()

    await auth.login('admin@enterprisestarter.local', 'password')
    await auth.logout()

    expect(auth.user).toBeNull()
    expect(auth.isAuthenticated).toBe(false)
    expect(auth.error).toBeNull()
  })
})
