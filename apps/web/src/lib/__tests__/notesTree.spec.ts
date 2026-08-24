import { describe, expect, it } from 'vitest'
import { ancestorNoteIds, buildNotesTree } from '../notesTree'

describe('buildNotesTree', () => {
  it('nests a note under another note and keeps a missing parent as a root', () => {
    const tree = buildNotesTree([
      { id: 'root', title: 'Reading list', parentDocumentId: null },
      { id: 'child', title: 'Chapter one', parentDocumentId: 'root' },
      { id: 'orphan', title: 'Loose note', parentDocumentId: 'missing' },
    ])

    expect(tree.map((node) => node.id)).toEqual(['orphan', 'root'])
    expect(tree[1]?.children.map((node) => node.id)).toEqual(['child'])
  })
})

describe('ancestorNoteIds', () => {
  it('returns parents from the selected note up to the root', () => {
    expect(
      ancestorNoteIds(
        [
          { id: 'root', parentDocumentId: null },
          { id: 'mid', parentDocumentId: 'root' },
          { id: 'leaf', parentDocumentId: 'mid' },
        ],
        'leaf',
      ),
    ).toEqual(['mid', 'root'])
  })
})
