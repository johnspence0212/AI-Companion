import type { ActivityItem } from '@/api/activityApi'
import type { LibraryDocument } from '@/api/documentsApi'
import type { Issue } from '@/api/issuesApi'
import type { SavedView } from '@/api/viewsApi'

export function applyDocumentView(
  documents: LibraryDocument[],
  view: SavedView | null,
): LibraryDocument[] {
  if (!view) {
    return documents
  }

  const folder = view.filters.folder
  if (folder === 'unfiled') {
    return documents.filter((document) => document.folderId === null)
  }

  if (folder && folder !== 'all') {
    return documents.filter((document) => document.folderId === folder)
  }

  return documents
}

export function applyIssueView(issues: Issue[], view: SavedView | null): Issue[] {
  if (!view) {
    return issues
  }

  const status = view.filters.status
  return status ? issues.filter((issue) => issue.status === status) : issues
}

export function applyActivityView(items: ActivityItem[], view: SavedView | null): ActivityItem[] {
  if (!view) {
    return items
  }

  const recordType = view.filters.recordType
  return recordType ? items.filter((item) => item.recordType === recordType) : items
}

export function groupsByStatus<T extends { status: string }>(
  items: T[],
  statuses: readonly string[],
): Array<{ status: string; items: T[] }> {
  return statuses.map((status) => ({
    status,
    items: items.filter((item) => item.status === status),
  }))
}
