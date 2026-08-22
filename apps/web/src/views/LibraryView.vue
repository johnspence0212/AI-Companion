<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import {
  documentsApi,
  type DocumentRevision,
  type LibraryDocument,
  type LibraryFolder,
} from '@/api/documentsApi'
import { viewsApi, type SavedView } from '@/api/viewsApi'
import { applyDocumentView } from '@/lib/savedViews'
import {
  Button,
  DataList,
  DataListEmpty,
  DataListItem,
  FormField,
  FormSection,
  FormSlideout,
  Input,
  MarkdownSource,
  PageBody,
  PageHeader,
  SavedViewBar,
  StatusMessage,
  WorkbenchPanes,
} from '@/ui'

const documents = ref<LibraryDocument[]>([])
const folders = ref<LibraryFolder[]>([])
const views = ref<SavedView[]>([])
const revisions = ref<DocumentRevision[]>([])
const selectedFolderId = ref<string | 'all' | 'unfiled'>('all')
const selectedViewId = ref<string | null>(null)
const selectedId = ref<string | null>(null)
const slideoutOpen = ref(false)
const filterSlideoutOpen = ref(false)
const filterName = ref('')
const filterFolder = ref('all')
const editingId = ref<string | null>(null)
const title = ref('')
const body = ref('')
const revisionId = ref('')
const folderDraft = ref('')
const loading = ref(false)
const saving = ref(false)
const error = ref<string | null>(null)
const notice = ref<string | null>(null)

const selected = computed(
  () => documents.value.find((document) => document.id === selectedId.value) ?? null,
)

const selectedView = computed(
  () => views.value.find((view) => view.id === selectedViewId.value) ?? null,
)

const visibleDocuments = computed(() => {
  const fromView = applyDocumentView(documents.value, selectedView.value)
  if (selectedFolderId.value === 'all') {
    return fromView
  }

  if (selectedFolderId.value === 'unfiled') {
    return fromView.filter((document) => document.folderId === null)
  }

  return fromView.filter((document) => document.folderId === selectedFolderId.value)
})

const folderLabels = computed(() => {
  const byId = new Map(folders.value.map((folder) => [folder.id, folder]))
  return folders.value
    .slice()
    .sort((left, right) => left.rank - right.rank || left.name.localeCompare(right.name))
    .map((folder) => {
      const parts = [folder.name]
      let parentId = folder.parentFolderId
      while (parentId) {
        const parent = byId.get(parentId)
        if (!parent) {
          break
        }
        parts.unshift(parent.name)
        parentId = parent.parentFolderId
      }

      return { ...folder, path: parts.join(' / ') }
    })
})

function folderName(id: string | null) {
  if (!id) {
    return 'Unfiled'
  }

  return folderLabels.value.find((folder) => folder.id === id)?.path ?? 'Folder'
}

async function loadLibrary() {
  loading.value = true
  error.value = null
  try {
    const [documentItems, folderItems, viewItems] = await Promise.all([
      documentsApi.list(),
      documentsApi.listFolders(),
      viewsApi.list('Documents'),
    ])
    documents.value = documentItems
    folders.value = folderItems
    views.value = viewItems
    if (!selectedViewId.value || !viewItems.some((view) => view.id === selectedViewId.value)) {
      selectedViewId.value = viewItems[0]?.id ?? null
    }
    if (selectedId.value && !documentItems.some((document) => document.id === selectedId.value)) {
      selectedId.value = null
      revisions.value = []
    }
  } catch (e) {
    error.value = e instanceof Error ? e.message : 'Failed to load the library'
    throw e
  } finally {
    loading.value = false
  }
}

async function selectDocument(document: LibraryDocument) {
  selectedId.value = document.id
  notice.value = null
  revisions.value = await documentsApi.revisions(document.id)
}

function openCreate() {
  editingId.value = null
  title.value = ''
  body.value = ''
  revisionId.value = ''
  revisions.value = []
  slideoutOpen.value = true
}

function openEdit(document: LibraryDocument) {
  editingId.value = document.id
  title.value = document.title
  body.value = document.body
  revisionId.value = document.revisionId
  slideoutOpen.value = true
  void selectDocument(document)
}

function setSlideoutOpen(open: boolean) {
  slideoutOpen.value = open
}

async function save() {
  if (!title.value.trim()) {
    error.value = 'Title is required.'
    return
  }

  saving.value = true
  error.value = null
  notice.value = null
  try {
    const folderId =
      selectedFolderId.value === 'all' || selectedFolderId.value === 'unfiled'
        ? null
        : selectedFolderId.value
    const saved = editingId.value
      ? await documentsApi.save(editingId.value, revisionId.value, title.value, body.value)
      : await documentsApi.create(title.value, body.value, folderId)
    await loadLibrary()
    await selectDocument(saved)
    slideoutOpen.value = false
    notice.value = 'Saved.'
  } catch (e) {
    error.value = e instanceof Error ? e.message : 'Failed to save document'
    throw e
  } finally {
    saving.value = false
  }
}

async function restore(revision: DocumentRevision) {
  if (!editingId.value) {
    return
  }

  saving.value = true
  error.value = null
  try {
    const restored = await documentsApi.restore(editingId.value, revisionId.value, revision.id)
    await loadLibrary()
    await selectDocument(restored)
    title.value = restored.title
    body.value = restored.body
    revisionId.value = restored.revisionId
    notice.value = 'Restored a new current revision.'
  } catch (e) {
    error.value = e instanceof Error ? e.message : 'Failed to restore revision'
    throw e
  } finally {
    saving.value = false
  }
}

async function addFolder() {
  if (!folderDraft.value.trim()) {
    return
  }

  saving.value = true
  error.value = null
  try {
    const created = await documentsApi.createFolder(folderDraft.value.trim())
    folderDraft.value = ''
    await loadLibrary()
    selectedFolderId.value = created.id
  } catch (e) {
    error.value = e instanceof Error ? e.message : 'Failed to create folder'
    throw e
  } finally {
    saving.value = false
  }
}

async function duplicateView() {
  if (!selectedViewId.value) {
    return
  }

  saving.value = true
  error.value = null
  try {
    const copy = await viewsApi.duplicate(selectedViewId.value)
    views.value = await viewsApi.list('Documents')
    selectedViewId.value = copy.id
    notice.value = 'Duplicated Saved View. Edit filters on the copy.'
  } catch (e) {
    error.value = e instanceof Error ? e.message : 'Failed to duplicate Saved View'
    throw e
  } finally {
    saving.value = false
  }
}

function openFilterEditor() {
  if (!selectedView.value || selectedView.value.isSystem) {
    return
  }

  filterName.value = selectedView.value.name
  filterFolder.value = selectedView.value.filters.folder ?? 'all'
  filterSlideoutOpen.value = true
}

async function saveFilters() {
  if (!selectedView.value || selectedView.value.isSystem) {
    return
  }

  saving.value = true
  error.value = null
  try {
    const updated = await viewsApi.update(selectedView.value.id, {
      name: filterName.value,
      filters: filterFolder.value === 'all' ? {} : { folder: filterFolder.value },
    })
    views.value = await viewsApi.list('Documents')
    selectedViewId.value = updated.id
    filterSlideoutOpen.value = false
    notice.value = 'Saved View filters updated.'
  } catch (e) {
    error.value = e instanceof Error ? e.message : 'Failed to update Saved View'
    throw e
  } finally {
    saving.value = false
  }
}

onMounted(() => {
  void loadLibrary()
})
</script>

<template>
  <PageBody>
    <PageHeader
      title="Library"
      description="Independent Documents. Source Markdown is the editor; fenced code is stored exactly."
    >
      <template #actions>
        <Button shape="square" @click="openCreate">New document</Button>
      </template>
    </PageHeader>

    <StatusMessage v-if="error" tone="error">{{ error }}</StatusMessage>
    <StatusMessage v-else-if="notice" tone="success">{{ notice }}</StatusMessage>

    <WorkbenchPanes
      :system-view="selectedView?.name ?? 'All Documents'"
      nav-title="Folders"
      list-title="Documents"
      detail-title="Source"
    >
      <template #nav>
        <form @submit.prevent="addFolder">
          <FormField label="New folder">
            <Input v-model="folderDraft" name="folder" autocomplete="off" />
          </FormField>
          <Button
            class="mt-3"
            type="submit"
            size="sm"
            shape="square"
            :disabled="saving || !folderDraft.trim()"
          >
            Add folder
          </Button>
        </form>
        <DataList class="mt-4">
          <DataListItem
            title="All"
            interactive
            :selected="selectedFolderId === 'all'"
            @click="selectedFolderId = 'all'"
          />
          <DataListItem
            title="Unfiled"
            interactive
            :selected="selectedFolderId === 'unfiled'"
            @click="selectedFolderId = 'unfiled'"
          />
          <DataListItem
            v-for="folder in folderLabels"
            :key="folder.id"
            :title="folder.path"
            interactive
            :selected="selectedFolderId === folder.id"
            @click="selectedFolderId = folder.id"
          />
        </DataList>
      </template>

      <template #list>
        <SavedViewBar
          class="mb-4"
          :views="views"
          :selected-id="selectedViewId"
          :pending="saving"
          @select="selectedViewId = $event"
          @duplicate="duplicateView"
          @edit="openFilterEditor"
        />
        <DataList>
          <DataListEmpty v-if="!loading && visibleDocuments.length === 0">
            No documents in this folder.
          </DataListEmpty>
          <DataListItem
            v-for="document in visibleDocuments"
            :key="document.id"
            :title="document.title"
            :description="folderName(document.folderId)"
            interactive
            :selected="selectedId === document.id"
            @click="selectDocument(document)"
          >
            <template #actions>
              <Button size="sm" variant="outline" shape="square" @click.stop="openEdit(document)">
                Edit
              </Button>
            </template>
          </DataListItem>
        </DataList>
      </template>

      <template #detail>
        <StatusMessage v-if="!selected"
          >Select a Document to read its source Markdown.</StatusMessage
        >
        <MarkdownSource v-else :model-value="selected.body" :label="selected.title" readonly />
      </template>
    </WorkbenchPanes>

    <FormSlideout
      :open="slideoutOpen"
      :title="editingId ? 'Edit Document' : 'New Document'"
      description="Source Markdown is stored exactly. Preview, highlight, and copy do not change it."
      :submit-label="editingId ? 'Save' : 'Create'"
      :pending="saving"
      allow-fullscreen
      size="wide"
      @update:open="setSlideoutOpen"
      @submit="save"
    >
      <FormSection title="Document">
        <FormField label="Title" required>
          <Input v-model="title" name="title" autocomplete="off" />
        </FormField>
        <MarkdownSource v-model="body" />
      </FormSection>
      <FormSection v-if="editingId && revisions.length" title="Revisions">
        <DataList>
          <DataListItem
            v-for="revision in revisions"
            :key="revision.id"
            :title="revision.kind"
            :description="revision.createdAt"
          >
            <template v-if="revision.id !== revisionId" #actions>
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
      </FormSection>
    </FormSlideout>

    <FormSlideout
      :open="filterSlideoutOpen"
      title="Edit Saved View"
      description="Changes apply to this copy. System Saved Views stay read-only."
      submit-label="Save"
      :pending="saving"
      @update:open="filterSlideoutOpen = $event"
      @submit="saveFilters"
    >
      <FormSection title="Filters">
        <FormField label="Name" required>
          <Input v-model="filterName" name="view-name" autocomplete="off" />
        </FormField>
        <FormField label="Folder">
          <select v-model="filterFolder">
            <option value="all">All</option>
            <option value="unfiled">Unfiled</option>
            <option v-for="folder in folderLabels" :key="folder.id" :value="folder.id">
              {{ folder.path }}
            </option>
          </select>
        </FormField>
      </FormSection>
    </FormSlideout>
  </PageBody>
</template>
