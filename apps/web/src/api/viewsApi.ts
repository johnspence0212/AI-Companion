import { ApiError, httpClient } from '@/api/base/client'
import { isRecord } from '@/api/types/schema'

export type SavedViewEntityType = 'Documents' | 'Issues' | 'Activity' | 'Projects'

export interface SavedViewSort {
  field: string
  direction: string
}

export interface SavedView {
  id: string
  name: string
  entityType: SavedViewEntityType
  projectId: string | null
  columns: string[]
  filters: Record<string, string>
  sort: SavedViewSort[]
  groupBy: string | null
  isSystem: boolean
}

function stringRecord(value: unknown): Record<string, string> {
  if (!isRecord(value)) {
    return {}
  }

  const filters: Record<string, string> = {}
  for (const [key, item] of Object.entries(value)) {
    if (typeof item === 'string') {
      filters[key] = item
    }
  }

  return filters
}

function parseSort(value: unknown): SavedViewSort[] {
  if (!Array.isArray(value)) {
    return []
  }

  return value.flatMap((item) => {
    if (!isRecord(item) || typeof item.field !== 'string') {
      return []
    }

    return [
      { field: item.field, direction: typeof item.direction === 'string' ? item.direction : 'asc' },
    ]
  })
}

function parseView(data: unknown): SavedView {
  if (!isRecord(data) || typeof data.id !== 'string' || typeof data.name !== 'string') {
    throw new ApiError('Unexpected saved view response from the server')
  }

  const entityType = data.entityType
  if (
    entityType !== 'Documents' &&
    entityType !== 'Issues' &&
    entityType !== 'Activity' &&
    entityType !== 'Projects'
  ) {
    throw new ApiError('Unexpected saved view entity type from the server')
  }

  return {
    id: data.id,
    name: data.name,
    entityType,
    projectId: typeof data.projectId === 'string' ? data.projectId : null,
    columns: Array.isArray(data.columns)
      ? data.columns.filter((item): item is string => typeof item === 'string')
      : [],
    filters: stringRecord(data.filters),
    sort: parseSort(data.sort),
    groupBy: typeof data.groupBy === 'string' ? data.groupBy : null,
    isSystem: data.isSystem === true,
  }
}

export const viewsApi = {
  list: async (entityType: SavedViewEntityType, projectId?: string): Promise<SavedView[]> => {
    const params = new URLSearchParams({ entityType })
    if (projectId) {
      params.set('projectId', projectId)
    }

    const data = await httpClient.get<unknown>(`/views?${params.toString()}`)
    return Array.isArray(data) ? data.map(parseView) : []
  },
  duplicate: async (id: string, name?: string, projectId?: string): Promise<SavedView> =>
    parseView(await httpClient.post(`/views/${id}/duplicate`, { name, projectId })),
  update: async (
    id: string,
    body: {
      name: string
      columns?: string[]
      filters?: Record<string, string>
      sort?: SavedViewSort[]
      groupBy?: string | null
    },
  ): Promise<SavedView> => parseView(await httpClient.put(`/views/${id}`, body)),
}
