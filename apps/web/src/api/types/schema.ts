import { ApiError } from '@/api/base/client'
import type { components } from '@/api/generated/schema'

export type User = components['schemas']['UserResponse']

export interface ManagedUser {
  id: string
  email: string
  displayName?: string | null
  roles: string[]
  isDisabled: boolean
  mustChangePassword: boolean
  createdAt?: string | null
  lastLoginAt?: string | null
}

export function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null
}

export function stringArray(value: unknown): string[] {
  return Array.isArray(value)
    ? value.filter((item): item is string => typeof item === 'string')
    : []
}

export function parseUser(data: unknown): User {
  if (!isRecord(data) || typeof data.id !== 'string' || typeof data.email !== 'string') {
    throw new ApiError('Unexpected user response from the server')
  }

  return {
    id: data.id,
    email: data.email,
    displayName: typeof data.displayName === 'string' ? data.displayName : null,
    isActive: data.isActive !== false,
    roles: stringArray(data.roles),
    permissions: stringArray(data.permissions),
    mustChangePassword: data.mustChangePassword === true,
    createdAt: typeof data.createdAt === 'string' ? data.createdAt : '',
    lastLoginAt: typeof data.lastLoginAt === 'string' ? data.lastLoginAt : null,
  }
}

export function parseManagedUser(data: unknown): ManagedUser {
  if (!isRecord(data) || typeof data.id !== 'string' || typeof data.email !== 'string') {
    throw new ApiError('Unexpected managed user response from the server')
  }

  return {
    id: data.id,
    email: data.email,
    displayName: (data.displayName as string | null | undefined) ?? undefined,
    roles: stringArray(data.roles),
    isDisabled: data.isDisabled === true || data.isActive === false,
    mustChangePassword: data.mustChangePassword === true,
    createdAt: (data.createdAt as string | null | undefined) ?? undefined,
    lastLoginAt: (data.lastLoginAt as string | null | undefined) ?? undefined,
  }
}
