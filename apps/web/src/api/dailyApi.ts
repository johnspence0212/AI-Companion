import { ApiError, httpClient } from '@/api/base/client'
import { isRecord } from '@/api/types/schema'

export interface DailyItem {
  id: string
  date: string
  rank: number
  issueId: string | null
  issueTitle: string | null
  issueStatus: string | null
  customText: string | null
  completedAt: string | null
}

export interface DailyBlocked {
  issueId: string
  projectId: string
  title: string
  status: string
  blockedReason: string | null
}

export interface Daily {
  date: string
  items: DailyItem[]
  carryover: DailyItem[]
  blocked: DailyBlocked[]
}

export function localDateOnly(now = new Date()): string {
  const year = now.getFullYear()
  const month = String(now.getMonth() + 1).padStart(2, '0')
  const day = String(now.getDate()).padStart(2, '0')
  return `${year}-${month}-${day}`
}

function readStatus(value: unknown): string | null {
  if (typeof value === 'string') {
    return value
  }

  if (typeof value === 'number') {
    return ['Backlog', 'Ready', 'Active', 'Blocked', 'Done', 'Canceled'][value] ?? String(value)
  }

  return null
}

function parseItem(data: unknown): DailyItem {
  if (!isRecord(data) || typeof data.id !== 'string' || typeof data.date !== 'string') {
    throw new ApiError('Unexpected daily item response from the server')
  }

  return {
    id: data.id,
    date: data.date,
    rank: typeof data.rank === 'number' ? data.rank : 0,
    issueId: typeof data.issueId === 'string' ? data.issueId : null,
    issueTitle: typeof data.issueTitle === 'string' ? data.issueTitle : null,
    issueStatus: readStatus(data.issueStatus),
    customText: typeof data.customText === 'string' ? data.customText : null,
    completedAt: typeof data.completedAt === 'string' ? data.completedAt : null,
  }
}

function parseBlocked(data: unknown): DailyBlocked {
  if (!isRecord(data) || typeof data.issueId !== 'string' || typeof data.title !== 'string') {
    throw new ApiError('Unexpected blocked issue response from the server')
  }

  return {
    issueId: data.issueId,
    projectId: typeof data.projectId === 'string' ? data.projectId : '',
    title: data.title,
    status: readStatus(data.status) ?? 'Blocked',
    blockedReason: typeof data.blockedReason === 'string' ? data.blockedReason : null,
  }
}

function parseDaily(data: unknown): Daily {
  if (!isRecord(data) || typeof data.date !== 'string') {
    throw new ApiError('Unexpected daily response from the server')
  }

  return {
    date: data.date,
    items: Array.isArray(data.items) ? data.items.map(parseItem) : [],
    carryover: Array.isArray(data.carryover) ? data.carryover.map(parseItem) : [],
    blocked: Array.isArray(data.blocked) ? data.blocked.map(parseBlocked) : [],
  }
}

export const dailyApi = {
  get: async (date: string): Promise<Daily> =>
    parseDaily(await httpClient.get(`/daily?date=${encodeURIComponent(date)}`)),
  addItem: async (date: string, customText: string): Promise<DailyItem> =>
    parseItem(await httpClient.post('/daily/items', { date, customText })),
  complete: async (id: string): Promise<DailyItem> =>
    parseItem(await httpClient.post(`/daily/items/${id}/complete`)),
}
