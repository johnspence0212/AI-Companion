import { ApiError, httpClient } from '@/api/base/client'
import { isRecord } from '@/api/types/schema'

export interface AiClient {
  id: string
  name: string
  createdAt: string
  archivedAt: string | null
}

export interface CreatedAiClient extends AiClient {
  secret: string
}

function parseClient(data: unknown): AiClient {
  if (!isRecord(data) || typeof data.id !== 'string' || typeof data.name !== 'string') {
    throw new ApiError('Unexpected AI Client response from the server')
  }

  return {
    id: data.id,
    name: data.name,
    createdAt: typeof data.createdAt === 'string' ? data.createdAt : '',
    archivedAt: typeof data.archivedAt === 'string' ? data.archivedAt : null,
  }
}

function parseCreated(data: unknown): CreatedAiClient {
  const client = parseClient(data)
  if (!isRecord(data) || typeof data.secret !== 'string' || data.secret.length === 0) {
    throw new ApiError('The server did not return a one-time AI Client secret')
  }

  return { ...client, secret: data.secret }
}

export const aiClientsApi = {
  list: async (): Promise<AiClient[]> => {
    const data = await httpClient.get<unknown>('/ai-clients')
    return Array.isArray(data) ? data.map(parseClient) : []
  },
  create: async (name: string): Promise<CreatedAiClient> =>
    parseCreated(await httpClient.post('/ai-clients', { name })),
  revoke: async (id: string): Promise<void> => {
    await httpClient.post(`/ai-clients/${id}/revoke`)
  },
}
