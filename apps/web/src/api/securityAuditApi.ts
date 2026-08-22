import { httpClient } from '@/api/base/client'
import { isRecord } from '@/api/types/schema'

export interface SecurityAuditEvent {
  id: number
  occurredAt: string
  eventType: string
  outcome: string
  actorEmail?: string | null
  subjectId?: string | null
  ipAddress?: string | null
}

export interface SecurityAuditPage {
  items: SecurityAuditEvent[]
  page: number
  pageSize: number
  total: number
}

function parseAuditEvent(value: unknown): SecurityAuditEvent | null {
  if (
    !isRecord(value) ||
    typeof value.id !== 'number' ||
    typeof value.occurredAt !== 'string' ||
    typeof value.eventType !== 'string' ||
    typeof value.outcome !== 'string'
  ) {
    return null
  }
  return {
    id: value.id,
    occurredAt: value.occurredAt,
    eventType: value.eventType,
    outcome: value.outcome,
    actorEmail: typeof value.actorEmail === 'string' ? value.actorEmail : null,
    subjectId: typeof value.subjectId === 'string' ? value.subjectId : null,
    ipAddress: typeof value.ipAddress === 'string' ? value.ipAddress : null,
  }
}

function parseAuditPage(value: unknown): SecurityAuditPage {
  const rawItems = Array.isArray(value)
    ? value
    : isRecord(value) && Array.isArray(value.items)
      ? value.items
      : []
  return {
    items: rawItems
      .map((item) => parseAuditEvent(item))
      .filter((item): item is SecurityAuditEvent => item !== null),
    page: isRecord(value) && typeof value.page === 'number' ? value.page : 1,
    pageSize: isRecord(value) && typeof value.pageSize === 'number' ? value.pageSize : 50,
    total: isRecord(value) && typeof value.total === 'number' ? value.total : rawItems.length,
  }
}

export const securityAuditApi = {
  list: async (page = 1): Promise<SecurityAuditPage> => {
    return parseAuditPage(await httpClient.get<unknown>(`/audit?page=${page}`))
  },
}
