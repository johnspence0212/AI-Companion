import { clearCsrfToken, httpClient } from '@/api/base/client'
import type { components } from '@/api/generated/schema'
import { parseUser, type User } from '@/api/types/schema'

export type LoginRequest = components['schemas']['LoginRequest']
export type ChangePasswordRequest = components['schemas']['ChangePasswordRequest']
export type UpdateProfileRequest = components['schemas']['UpdateProfileRequest']

export const authApi = {
  login: async (data: LoginRequest): Promise<User> => {
    clearCsrfToken()
    await httpClient.post<void>('/auth/login', data)
    // Authentication changes the antiforgery token identity; fetch a fresh pair for later writes.
    clearCsrfToken()
    return parseUser(await httpClient.get<unknown>('/auth/me'))
  },

  me: async (): Promise<User> => parseUser(await httpClient.get<unknown>('/auth/me')),

  updateProfile: async (data: UpdateProfileRequest): Promise<User> =>
    parseUser(await httpClient.put<unknown, UpdateProfileRequest>('/auth/profile', data)),

  changePassword: async (data: ChangePasswordRequest): Promise<User> => {
    await httpClient.post<void>('/auth/change-password', data)
    return parseUser(await httpClient.get<unknown>('/auth/me'))
  },

  logout: async (): Promise<void> => {
    try {
      await httpClient.post<void>('/auth/logout')
    } finally {
      clearCsrfToken()
    }
  },
}
