export interface NotesTreeNode {
  id: string
  title: string
  children: NotesTreeNode[]
}

export function buildNotesTree(
  documents: ReadonlyArray<{ id: string; title: string; parentDocumentId: string | null }>,
): NotesTreeNode[] {
  const ids = new Set(documents.map((document) => document.id))
  const grouped = new Map<
    string | null,
    { id: string; title: string; parentDocumentId: string | null }[]
  >()
  for (const document of documents) {
    const parentId =
      document.parentDocumentId && ids.has(document.parentDocumentId)
        ? document.parentDocumentId
        : null
    const siblings = grouped.get(parentId) ?? []
    siblings.push(document)
    grouped.set(parentId, siblings)
  }

  const walk = (parentId: string | null): NotesTreeNode[] =>
    (grouped.get(parentId) ?? [])
      .slice()
      .sort((left, right) => left.title.localeCompare(right.title))
      .map((document) => ({
        id: document.id,
        title: document.title,
        children: walk(document.id),
      }))

  return walk(null)
}

export function ancestorNoteIds(
  documents: ReadonlyArray<{ id: string; parentDocumentId: string | null }>,
  id: string,
): string[] {
  const byId = new Map(documents.map((document) => [document.id, document]))
  const ids: string[] = []
  const seen = new Set<string>()
  let current = byId.get(id)?.parentDocumentId ?? null
  while (current) {
    if (seen.has(current)) {
      break
    }

    seen.add(current)
    ids.push(current)
    current = byId.get(current)?.parentDocumentId ?? null
  }

  return ids
}
