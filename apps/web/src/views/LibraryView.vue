<script setup lang="ts">
import { computed, onMounted, onUnmounted, ref } from 'vue'
import { documentsApi, type DocumentRevision, type LibraryDocument } from '@/api/documentsApi'
import { onDocumentsChanged } from '@/lib/libraryEvents'
import { ancestorNoteIds, buildNotesTree } from '@/lib/notesTree'
import {
  Button,
  DataList,
  DataListEmpty,
  DataListItem,
  FormField,
  Input,
  MarkdownSource,
  NotesTree,
  PageBody,
  StatusMessage,
  WorkbenchPanes,
  WorkbenchSection,
} from '@/ui'

const documents = ref<LibraryDocument[]>([])
const revisions = ref<DocumentRevision[]>([])
const selectedId = ref<string | null>(null)
const expandedIds = ref<string[]>([])
const draftTitle = ref('')
const draftBody = ref('')
const draftRevisionId = ref('')
const loading = ref(false)
const saving = ref(false)
const error = ref<string | null>(null)
const notice = ref<string | null>(null)

const selected = computed(
  () => documents.value.find((document) => document.id === selectedId.value) ?? null,
)

const tree = computed(() => buildNotesTree(documents.value))

const isDirty = computed(() => {
  if (!selected.value) {
    return false
  }

  return draftTitle.value !== selected.value.title || draftBody.value !== selected.value.body
})

function expandAncestors(id: string) {
  expandedIds.value = [...new Set([...expandedIds.value, ...ancestorNoteIds(documents.value, id)])]
}

function applyDraft(document: LibraryDocument) {
  selectedId.value = document.id
  draftTitle.value = document.title
  draftBody.value = document.body
  draftRevisionId.value = document.revisionId
  expandAncestors(document.id)
}

async function loadLibrary() {
  loading.value = true
  error.value = null
  try {
    const documentItems = await documentsApi.list()
    documents.value = documentItems
    if (selectedId.value && !documentItems.some((document) => document.id === selectedId.value)) {
      selectedId.value = null
      revisions.value = []
      draftTitle.value = ''
      draftBody.value = ''
      draftRevisionId.value = ''
    }
  } catch (e) {
    error.value = e instanceof Error ? e.message : 'Failed to load notes'
    throw e
  } finally {
    loading.value = false
  }
}

async function selectNote(document: LibraryDocument) {
  if (selected.value && isDirty.value && selected.value.id !== document.id) {
    await saveCurrent()
  }

  notice.value = null
  applyDraft(document)
  revisions.value = await documentsApi.revisions(document.id)
}

async function selectNoteById(id: string) {
  const document = documents.value.find((item) => item.id === id)
  if (document) {
    await selectNote(document)
  }
}

async function saveCurrent() {
  if (!selected.value) {
    return null
  }

  const title = draftTitle.value.trim() || 'Untitled'
  saving.value = true
  error.value = null
  try {
    const saved = await documentsApi.save(
      selected.value.id,
      draftRevisionId.value,
      title,
      draftBody.value,
    )
    await loadLibrary()
    applyDraft(saved)
    revisions.value = await documentsApi.revisions(saved.id)
    notice.value = 'Saved.'
    return saved
  } catch (e) {
    error.value = e instanceof Error ? e.message : 'Failed to save note'
    throw e
  } finally {
    saving.value = false
  }
}

async function addNote(parentId?: string) {
  if (selected.value && isDirty.value) {
    await saveCurrent()
  }

  saving.value = true
  error.value = null
  notice.value = null
  try {
    if (parentId) {
      expandedIds.value = [...new Set([...expandedIds.value, parentId])]
    }

    const created = await documentsApi.create({
      title: 'Untitled',
      body: '',
      parentDocumentId: parentId ?? null,
    })
    await loadLibrary()
    await selectNote(created)
  } catch (e) {
    error.value = e instanceof Error ? e.message : 'Failed to create note'
    throw e
  } finally {
    saving.value = false
  }
}

function toggleExpanded(id: string) {
  expandedIds.value = expandedIds.value.includes(id)
    ? expandedIds.value.filter((item) => item !== id)
    : [...expandedIds.value, id]
}

async function restore(revision: DocumentRevision) {
  if (!selected.value) {
    return
  }

  saving.value = true
  error.value = null
  try {
    const restored = await documentsApi.restore(
      selected.value.id,
      draftRevisionId.value,
      revision.id,
    )
    await loadLibrary()
    applyDraft(restored)
    revisions.value = await documentsApi.revisions(restored.id)
    notice.value = 'Restored a new current revision.'
  } catch (e) {
    error.value = e instanceof Error ? e.message : 'Failed to restore revision'
    throw e
  } finally {
    saving.value = false
  }
}

onMounted(() => {
  void loadLibrary()
})

onUnmounted(
  onDocumentsChanged(() => {
    void loadLibrary()
  }),
)
</script>

<template>
  <PageBody variant="workbench">
    <StatusMessage v-if="error" class="px-4 py-2" tone="error">{{ error }}</StatusMessage>
    <StatusMessage v-else-if="notice" class="px-4 py-2" tone="success">{{ notice }}</StatusMessage>

    <WorkbenchPanes list-title="Notes" :detail-title="selected?.title ?? 'Note'">
      <template #list-actions>
        <Button size="sm" shape="square" :disabled="saving" @click="addNote()">New note</Button>
      </template>
      <template #list>
        <DataList v-if="!loading && tree.length === 0" variant="flush">
          <DataListEmpty>No notes yet. Create one to start writing.</DataListEmpty>
        </DataList>
        <NotesTree
          v-else
          :items="tree"
          :selected-id="selectedId"
          :expanded-ids="expandedIds"
          :pending="saving"
          @select="selectNoteById"
          @add-child="addNote"
          @toggle="toggleExpanded"
        />
      </template>

      <template #detail-actions>
        <Button
          v-if="selected"
          size="sm"
          shape="square"
          :disabled="saving || !isDirty"
          @click="saveCurrent"
        >
          Save
        </Button>
      </template>
      <template #detail>
        <DataList v-if="!selected" variant="flush">
          <DataListEmpty>Select a note or create one.</DataListEmpty>
        </DataList>
        <div v-else class="space-y-3 p-3">
          <FormField label="Title" required>
            <Input v-model="draftTitle" name="title" autocomplete="off" />
          </FormField>
          <MarkdownSource v-model="draftBody" label="Note" />
          <WorkbenchSection v-if="revisions.length" title="History">
            <DataList variant="flush">
              <DataListItem
                v-for="revision in revisions"
                :key="revision.id"
                :title="revision.kind"
                :description="revision.createdAt"
              >
                <template v-if="revision.id !== draftRevisionId" #actions>
                  <Button
                    variant="outline"
                    size="sm"
                    shape="square"
                    :disabled="saving"
                    @click="restore(revision)"
                  >
                    Restore
                  </Button>
                </template>
              </DataListItem>
            </DataList>
          </WorkbenchSection>
        </div>
      </template>
    </WorkbenchPanes>
  </PageBody>
</template>
