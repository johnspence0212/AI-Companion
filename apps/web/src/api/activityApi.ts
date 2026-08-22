import { ApiError, httpClient } from '@/api/base/client'
import { isRecord } from '@/api/types/schema'

export interface ActivityItem {
  id: string
  occurredAt: string
  actorUserId: string
  actorAiClientId: string | null
  actionType: string
  recordType: string
  recordId: string
  projectId: string | null
  sessionId: string | null
  summary: string
}

function parseActivity(data: unknown): ActivityItem {
  if (!isRecord(data) || typeof data.id !== 'string' || typeof data.summary !== 'string') {
    throw new ApiError('Unexpected activity response from the server')
  }

  return {
    id: data.id,
    occurredAt: typeof data.occurredAt === 'string' ? data.occurredAt : '',
    actorUserId: typeof data.actorUserId === 'string' ? data.actorUserId : '',
    actorAiClientId: typeof data.actorAiClientId === 'string' ? data.actorAiClientId : null,
    actionType: typeof data.actionType === 'string' ? data.actionType : '',
    recordType: typeof data.recordType === 'string' ? data.recordType : '',
    recordId: typeof data.recordId === 'string' ? data.recordId : '',
    projectId: typeof data.projectId === 'string' ? data.projectId : null,
    sessionId: typeof data.sessionId === 'string' ? data.sessionId : null,
    summary: data.summary,
  }
}

export const activityApi = {
  list: async (projectId: string): Promise<ActivityItem[]> => {
    const data = await httpClient.get<unknown>(
      `/activity?projectId=${encodeURIComponent(projectId)}`,
    )
    return Array.isArray(data) ? data.map(parseActivity) : []
  },
}
