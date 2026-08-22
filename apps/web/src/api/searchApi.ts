import { ApiError, httpClient } from '@/api/base/client'
import { isRecord } from '@/api/types/schema'

export interface SearchHit {
  id: string
  title: string
  projectId: string | null
  updatedAt: string
  createdAt: string
}

export interface SearchResults {
  query: string
  projects: SearchHit[]
  documents: SearchHit[]
  issues: SearchHit[]
  activity: SearchHit[]
}

function parseHit(data: unknown): SearchHit {
  if (!isRecord(data) || typeof data.id !== 'string' || typeof data.title !== 'string') {
    throw new ApiError('Unexpected search hit from the server')
  }

  return {
    id: data.id,
    title: data.title,
    projectId: typeof data.projectId === 'string' ? data.projectId : null,
    updatedAt: typeof data.updatedAt === 'string' ? data.updatedAt : '',
    createdAt: typeof data.createdAt === 'string' ? data.createdAt : '',
  }
}

function parseHits(value: unknown): SearchHit[] {
  return Array.isArray(value) ? value.map(parseHit) : []
}

function parseResults(data: unknown): SearchResults {
  if (!isRecord(data)) {
    throw new ApiError('Unexpected search response from the server')
  }

  return {
    query: typeof data.query === 'string' ? data.query : '',
    projects: parseHits(data.projects),
    documents: parseHits(data.documents),
    issues: parseHits(data.issues),
    activity: parseHits(data.activity),
  }
}

export const searchApi = {
  query: async (q: string): Promise<SearchResults> =>
    parseResults(await httpClient.get(`/search?q=${encodeURIComponent(q)}`)),
}
