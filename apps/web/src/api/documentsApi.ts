import { ApiError, httpClient } from '@/api/base/client'
import { isRecord, stringArray } from '@/api/types/schema'

export interface LibraryDocument {
  id: string
  title: string
  slug: string | null
  body: string
  revisionId: string
  folderId: string | null
  parentDocumentId: string | null
  projectIds: string[]
  tags: string[]
  updatedAt: string
  archivedAt: string | null
}

export interface DocumentRevision {
  id: string
  title: string
  body: string
  kind: string
  createdAt: string
}

export interface LibraryFolder {
  id: string
  parentFolderId: string | null
  name: string
  rank: number
}

function parseDocument(data: unknown): LibraryDocument {
  if (
    !isRecord(data) ||
    typeof data.id !== 'string' ||
    typeof data.title !== 'string' ||
    typeof data.body !== 'string' ||
    typeof data.revisionId !== 'string'
  ) {
    throw new ApiError('Unexpected document response from the server')
  }

  return {
    id: data.id,
    title: data.title,
    slug: typeof data.slug === 'string' ? data.slug : null,
    body: data.body,
    revisionId: data.revisionId,
    folderId: typeof data.folderId === 'string' ? data.folderId : null,
    parentDocumentId: typeof data.parentDocumentId === 'string' ? data.parentDocumentId : null,
    projectIds: stringArray(data.projectIds),
    tags: stringArray(data.tags),
    updatedAt: typeof data.updatedAt === 'string' ? data.updatedAt : '',
    archivedAt: typeof data.archivedAt === 'string' ? data.archivedAt : null,
  }
}

function parseFolder(data: unknown): LibraryFolder {
  if (!isRecord(data) || typeof data.id !== 'string' || typeof data.name !== 'string') {
    throw new ApiError('Unexpected folder response from the server')
  }

  return {
    id: data.id,
    parentFolderId: typeof data.parentFolderId === 'string' ? data.parentFolderId : null,
    name: data.name,
    rank: typeof data.rank === 'number' ? data.rank : 0,
  }
}

function parseRevision(data: unknown): DocumentRevision {
  if (!isRecord(data) || typeof data.id !== 'string' || typeof data.body !== 'string') {
    throw new ApiError('Unexpected revision response from the server')
  }

  return {
    id: data.id,
    title: typeof data.title === 'string' ? data.title : '',
    body: data.body,
    kind: typeof data.kind === 'string' ? data.kind : 'save',
    createdAt: typeof data.createdAt === 'string' ? data.createdAt : '',
  }
}

export const documentsApi = {
  list: async (): Promise<LibraryDocument[]> => {
    const data = await httpClient.get<unknown>('/documents')
    return Array.isArray(data) ? data.map(parseDocument) : []
  },
  get: async (id: string): Promise<LibraryDocument> =>
    parseDocument(await httpClient.get(`/documents/${id}`)),
  create: async (input: {
    title: string
    body?: string
    folderId?: string | null
    parentDocumentId?: string | null
  }): Promise<LibraryDocument> =>
    parseDocument(
      await httpClient.post('/documents', {
        title: input.title,
        body: input.body ?? '',
        folderId: input.folderId,
        parentDocumentId: input.parentDocumentId,
      }),
    ),
  listFolders: async (): Promise<LibraryFolder[]> => {
    const data = await httpClient.get<unknown>('/folders')
    return Array.isArray(data) ? data.map(parseFolder) : []
  },
  createFolder: async (name: string, parentFolderId?: string | null): Promise<LibraryFolder> =>
    parseFolder(await httpClient.post('/folders', { name, parentFolderId })),
  save: async (
    id: string,
    expectedRevisionId: string,
    title: string,
    body: string,
  ): Promise<LibraryDocument> =>
    parseDocument(await httpClient.put(`/documents/${id}`, { expectedRevisionId, title, body })),
  revisions: async (id: string): Promise<DocumentRevision[]> => {
    const data = await httpClient.get<unknown>(`/documents/${id}/revisions`)
    return Array.isArray(data) ? data.map(parseRevision) : []
  },
  restore: async (
    id: string,
    expectedRevisionId: string,
    revisionId: string,
  ): Promise<LibraryDocument> =>
    parseDocument(
      await httpClient.post(`/documents/${id}/restore`, { expectedRevisionId, revisionId }),
    ),
}
