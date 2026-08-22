import { httpClient } from '@/api/base/client'
import { parseManagedUser, type ManagedUser } from '@/api/types/schema'

export interface CreateUserRequest {
  email: string
  temporaryPassword: string
  displayName?: string
  roles: string[]
}

export interface UpdateUserRequest {
  roles: string[]
  isDisabled: boolean
}

export const usersApi = {
  list: async (): Promise<ManagedUser[]> => {
    const data = await httpClient.get<unknown>('/users')
    const items =
      typeof data === 'object' && data !== null && 'items' in data && Array.isArray(data.items)
        ? data.items
        : []
    return items.map(parseManagedUser)
  },
  create: async (data: CreateUserRequest): Promise<ManagedUser> =>
    parseManagedUser(await httpClient.post<unknown>('/users', data)),
  update: async (userId: string, data: UpdateUserRequest): Promise<void> => {
    await httpClient.put(`/users/${userId}/roles`, { roles: data.roles })
    await httpClient.put(`/users/${userId}/active`, { isActive: !data.isDisabled })
  },
  resetPassword: (userId: string, temporaryPassword: string) =>
    httpClient.post<void>(`/users/${userId}/reset-password`, { temporaryPassword }),
}
