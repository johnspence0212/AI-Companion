import { ApiError, httpClient } from '@/api/base/client'
import { isRecord } from '@/api/types/schema'

export const ISSUE_STATUSES = ['Backlog', 'Ready', 'Active', 'Blocked', 'Done', 'Canceled'] as const

export type IssueStatusName = (typeof ISSUE_STATUSES)[number]

export interface Issue {
  id: string
  projectId: string
  title: string
  description: string | null
  status: IssueStatusName
  priority: string
  version: number
  blockedReason: string | null
  resolution: string | null
  effectivelyBlocked: boolean
  assigneeUserId: string | null
  assigneeAiClientId: string | null
}

const PRIORITIES = ['None', 'Low', 'Normal', 'High', 'Urgent'] as const

export function readIssueStatus(value: unknown): IssueStatusName {
  if (typeof value === 'string' && ISSUE_STATUSES.includes(value as IssueStatusName)) {
    return value as IssueStatusName
  }

  if (typeof value === 'number') {
    return ISSUE_STATUSES[value] ?? 'Backlog'
  }

  return 'Backlog'
}

function readPriority(value: unknown): string {
  if (typeof value === 'string') {
    return value
  }

  if (typeof value === 'number') {
    return PRIORITIES[value] ?? String(value)
  }

  return 'Normal'
}

function parseIssue(data: unknown): Issue {
  if (!isRecord(data) || typeof data.id !== 'string' || typeof data.title !== 'string') {
    throw new ApiError('Unexpected issue response from the server')
  }

  return {
    id: data.id,
    projectId: typeof data.projectId === 'string' ? data.projectId : '',
    title: data.title,
    description: typeof data.description === 'string' ? data.description : null,
    status: readIssueStatus(data.status),
    priority: readPriority(data.priority),
    version: typeof data.version === 'number' ? data.version : 0,
    blockedReason: typeof data.blockedReason === 'string' ? data.blockedReason : null,
    resolution: typeof data.resolution === 'string' ? data.resolution : null,
    effectivelyBlocked: data.effectivelyBlocked === true,
    assigneeUserId: typeof data.assigneeUserId === 'string' ? data.assigneeUserId : null,
    assigneeAiClientId:
      typeof data.assigneeAiClientId === 'string' ? data.assigneeAiClientId : null,
  }
}

export const issuesApi = {
  list: async (projectId: string): Promise<Issue[]> => {
    const data = await httpClient.get<unknown>(`/projects/${projectId}/issues`)
    return Array.isArray(data) ? data.map(parseIssue) : []
  },
  get: async (id: string): Promise<Issue> => parseIssue(await httpClient.get(`/issues/${id}`)),
  create: async (projectId: string, title: string, description?: string): Promise<Issue> =>
    parseIssue(await httpClient.post(`/projects/${projectId}/issues`, { title, description })),
}
