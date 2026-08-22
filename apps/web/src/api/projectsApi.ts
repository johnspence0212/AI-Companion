import { ApiError, httpClient } from '@/api/base/client'
import { isRecord } from '@/api/types/schema'

export interface Project {
  id: string
  name: string
  slug: string
  contextDocumentId: string
  version: number
  createdAt: string
  archivedAt: string | null
}

export interface ProjectContext {
  projectId: string
  projectSlug: string
  documentId: string
  revisionId: string
  title: string
  body: string
  updatedAt: string
}

function parseProject(data: unknown): Project {
  if (
    !isRecord(data) ||
    typeof data.id !== 'string' ||
    typeof data.name !== 'string' ||
    typeof data.slug !== 'string'
  ) {
    throw new ApiError('Unexpected project response from the server')
  }

  return {
    id: data.id,
    name: data.name,
    slug: data.slug,
    contextDocumentId: typeof data.contextDocumentId === 'string' ? data.contextDocumentId : '',
    version: typeof data.version === 'number' ? data.version : 0,
    createdAt: typeof data.createdAt === 'string' ? data.createdAt : '',
    archivedAt: typeof data.archivedAt === 'string' ? data.archivedAt : null,
  }
}

function parseContext(data: unknown): ProjectContext {
  if (
    !isRecord(data) ||
    typeof data.projectId !== 'string' ||
    typeof data.revisionId !== 'string' ||
    typeof data.body !== 'string'
  ) {
    throw new ApiError('Unexpected project context response from the server')
  }

  return {
    projectId: data.projectId,
    projectSlug: typeof data.projectSlug === 'string' ? data.projectSlug : '',
    documentId: typeof data.documentId === 'string' ? data.documentId : '',
    revisionId: data.revisionId,
    title: typeof data.title === 'string' ? data.title : '',
    body: data.body,
    updatedAt: typeof data.updatedAt === 'string' ? data.updatedAt : '',
  }
}

export const projectsApi = {
  list: async (): Promise<Project[]> => {
    const data = await httpClient.get<unknown>('/projects')
    return Array.isArray(data) ? data.map(parseProject) : []
  },
  get: async (idOrSlug: string): Promise<Project> =>
    parseProject(await httpClient.get(`/projects/${idOrSlug}`)),
  create: async (name: string): Promise<Project> =>
    parseProject(await httpClient.post('/projects', { name })),
  getContext: async (idOrSlug: string): Promise<ProjectContext> =>
    parseContext(await httpClient.get(`/projects/${idOrSlug}/context`)),
  saveContext: async (
    idOrSlug: string,
    expectedRevisionId: string,
    title: string,
    body: string,
  ): Promise<ProjectContext> =>
    parseContext(
      await httpClient.put(`/projects/${idOrSlug}/context`, {
        expectedRevisionId,
        title,
        body,
      }),
    ),
}
