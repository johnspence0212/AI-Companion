import { httpClient } from '@/api/base/client'

export interface PermissionDefinition {
  key: string
  label: string
  group: string
}

export interface Role {
  id: string
  name: string
  isProtected: boolean
  permissions: string[]
}

export interface SaveRoleRequest {
  name: string
  permissions: string[]
}

export const rolesApi = {
  catalog: () => httpClient.get<PermissionDefinition[]>('/roles/permissions'),
  list: async (): Promise<Role[]> => {
    const data = await httpClient.get<{ items: Role[] }>('/roles')
    return data.items
  },
  create: (data: SaveRoleRequest) => httpClient.post<Role>('/roles', data),
  update: (roleId: string, data: SaveRoleRequest) => httpClient.put(`/roles/${roleId}`, data),
  remove: (roleId: string) => httpClient.delete(`/roles/${roleId}`),
}
