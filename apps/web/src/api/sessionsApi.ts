import { ApiError, httpClient } from '@/api/base/client'
import { isRecord } from '@/api/types/schema'

export interface ProjectSession {
  id: string
  projectId: string
  actorUserId: string
  actorAiClientId: string | null
  startedAt: string
  finishedAt: string | null
  summary: string | null
}

function parseSession(data: unknown): ProjectSession {
  if (!isRecord(data) || typeof data.id !== 'string' || typeof data.startedAt !== 'string') {
    throw new ApiError('Unexpected session response from the server')
  }

  return {
    id: data.id,
    projectId: typeof data.projectId === 'string' ? data.projectId : '',
    actorUserId: typeof data.actorUserId === 'string' ? data.actorUserId : '',
    actorAiClientId: typeof data.actorAiClientId === 'string' ? data.actorAiClientId : null,
    startedAt: data.startedAt,
    finishedAt: typeof data.finishedAt === 'string' ? data.finishedAt : null,
    summary: typeof data.summary === 'string' ? data.summary : null,
  }
}

export const sessionsApi = {
  list: async (projectId: string): Promise<ProjectSession[]> => {
    const data = await httpClient.get<unknown>(`/projects/${projectId}/sessions`)
    return Array.isArray(data) ? data.map(parseSession) : []
  },
}
