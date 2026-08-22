import { ApiError, httpClient } from '@/api/base/client'
import { isRecord } from '@/api/types/schema'

export const INBOX_STATUSES = ['Open', 'Processed', 'Archived'] as const
export type InboxStatusName = (typeof INBOX_STATUSES)[number]

export interface InboxItem {
  id: string
  text: string
  status: InboxStatusName
  documentId: string | null
  issueId: string | null
  createdAt: string
  processedAt: string | null
  archivedAt: string | null
}

function readStatus(value: unknown): InboxStatusName {
  if (typeof value === 'string' && INBOX_STATUSES.includes(value as InboxStatusName)) {
    return value as InboxStatusName
  }

  if (typeof value === 'number') {
    return INBOX_STATUSES[value] ?? 'Open'
  }

  return 'Open'
}

function parseItem(data: unknown): InboxItem {
  if (!isRecord(data) || typeof data.id !== 'string' || typeof data.text !== 'string') {
    throw new ApiError('Unexpected inbox response from the server')
  }

  return {
    id: data.id,
    text: data.text,
    status: readStatus(data.status),
    documentId: typeof data.documentId === 'string' ? data.documentId : null,
    issueId: typeof data.issueId === 'string' ? data.issueId : null,
    createdAt: typeof data.createdAt === 'string' ? data.createdAt : '',
    processedAt: typeof data.processedAt === 'string' ? data.processedAt : null,
    archivedAt: typeof data.archivedAt === 'string' ? data.archivedAt : null,
  }
}

export const inboxApi = {
  list: async (): Promise<InboxItem[]> => {
    const data = await httpClient.get<unknown>('/inbox')
    return Array.isArray(data) ? data.map(parseItem) : []
  },
  capture: async (text: string): Promise<InboxItem> =>
    parseItem(await httpClient.post('/inbox', { text })),
  process: async (
    id: string,
    body: {
      title?: string
      projectId?: string
      createDocument?: boolean
      createIssue?: boolean
      documentId?: string
      issueId?: string
    },
  ): Promise<InboxItem> =>
    parseItem(
      await httpClient.post(`/inbox/${id}/process`, {
        title: body.title,
        projectId: body.projectId,
        createDocument: body.createDocument === true,
        createIssue: body.createIssue === true,
        documentId: body.documentId,
        issueId: body.issueId,
      }),
    ),
  archive: async (id: string): Promise<InboxItem> =>
    parseItem(await httpClient.post(`/inbox/${id}/archive`)),
}
