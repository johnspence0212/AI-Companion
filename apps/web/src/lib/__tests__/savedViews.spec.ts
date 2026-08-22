import { describe, expect, it } from 'vitest'
import type { LibraryDocument } from '@/api/documentsApi'
import type { SavedView } from '@/api/viewsApi'
import { applyDocumentView } from '../savedViews'

function view(filters: Record<string, string>): SavedView {
  return {
    id: 'view-1',
    name: 'Copy',
    entityType: 'Documents',
    projectId: null,
    columns: ['title'],
    filters,
    sort: [],
    groupBy: null,
    isSystem: false,
  }
}

function document(folderId: string | null): LibraryDocument {
  return {
    id: folderId ?? 'unfiled',
    title: 'Doc',
    slug: null,
    body: '',
    revisionId: 'rev',
    folderId,
    projectIds: [],
    tags: [],
    updatedAt: '',
    archivedAt: null,
  }
}

describe('applyDocumentView', () => {
  const documents = [document(null), document('folder-a')]

  it('keeps the system all-documents filter empty', () => {
    expect(applyDocumentView(documents, view({}))).toEqual(documents)
  })

  it('applies a duplicated unfiled filter without changing other views', () => {
    const original = view({})
    const copy = view({ folder: 'unfiled' })

    expect(applyDocumentView(documents, copy).map((item) => item.id)).toEqual(['unfiled'])
    expect(original.filters).toEqual({})
  })
})
