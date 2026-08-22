<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { documentsApi, type DocumentRevision, type LibraryDocument } from '@/api/documentsApi'
import {
  Button,
  DataList,
  DataListEmpty,
  DataListItem,
  FormField,
  Input,
  PageBody,
  PageHeader,
  StatusMessage,
  SurfaceCard,
  Textarea,
} from '@/ui'

const documents = ref<LibraryDocument[]>([])
const revisions = ref<DocumentRevision[]>([])
const selectedId = ref<string | null>(null)
const title = ref('')
const body = ref('')
const revisionId = ref('')
const loading = ref(false)
const saving = ref(false)
const error = ref<string | null>(null)
const notice = ref<string | null>(null)

async function loadList() {
  loading.value = true
  error.value = null
  try {
    documents.value = await documentsApi.list()
  } catch (e) {
    error.value = e instanceof Error ? e.message : 'Failed to load documents'
    throw e
  } finally {
    loading.value = false
  }
}

async function open(document: LibraryDocument) {
  selectedId.value = document.id
  title.value = document.title
  body.value = document.body
  revisionId.value = document.revisionId
  notice.value = null
  revisions.value = await documentsApi.revisions(document.id)
}

function startCreate() {
  selectedId.value = null
  title.value = ''
  body.value = ''
  revisionId.value = ''
  revisions.value = []
  notice.value = null
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
    const saved = selectedId.value
      ? await documentsApi.save(selectedId.value, revisionId.value, title.value, body.value)
      : await documentsApi.create(title.value, body.value)
    await loadList()
    await open(saved)
    notice.value = 'Saved.'
  } catch (e) {
    error.value = e instanceof Error ? e.message : 'Failed to save document'
    throw e
  } finally {
    saving.value = false
  }
}

async function restore(revision: DocumentRevision) {
  if (!selectedId.value) return
  saving.value = true
  error.value = null
  try {
    const restored = await documentsApi.restore(selectedId.value, revisionId.value, revision.id)
    await loadList()
    await open(restored)
    notice.value = 'Restored a new current revision.'
  } catch (e) {
    error.value = e instanceof Error ? e.message : 'Failed to restore revision'
    throw e
  } finally {
    saving.value = false
  }
}

onMounted(() => {
  void loadList()
})
</script>

<template>
  <PageBody>
    <PageHeader
      title="Library"
      description="Independent Documents. Source Markdown is the editor; fenced code is stored exactly."
    >
      <template #actions>
        <Button @click="startCreate">New document</Button>
      </template>
    </PageHeader>

    <StatusMessage v-if="error" tone="error">{{ error }}</StatusMessage>
    <StatusMessage v-else-if="notice" tone="success">{{ notice }}</StatusMessage>

    <div class="grid gap-6 md:grid-cols-2">
      <SurfaceCard>
        <DataList>
          <DataListEmpty v-if="!loading && documents.length === 0">No documents yet.</DataListEmpty>
          <DataListItem
            v-for="document in documents"
            :key="document.id"
            :title="document.title"
            class="cursor-pointer"
            @click="open(document)"
          />
        </DataList>
      </SurfaceCard>

      <SurfaceCard>
        <form class="space-y-4" @submit.prevent="save">
          <FormField label="Title">
            <Input v-model="title" name="title" autocomplete="off" />
          </FormField>
          <FormField label="Markdown">
            <Textarea v-model="body" name="body" rows="16" spellcheck="false" />
          </FormField>
          <Button type="submit" :disabled="saving">{{ selectedId ? 'Save' : 'Create' }}</Button>
        </form>

        <div v-if="revisions.length" class="mt-6 space-y-2">
          <h2 class="font-semibold">Revisions</h2>
          <DataList>
            <DataListItem
              v-for="revision in revisions"
              :key="revision.id"
              :title="revision.kind"
              :description="revision.createdAt"
            >
              <template v-if="revision.id !== revisionId" #actions>
                <Button variant="outline" size="sm" :disabled="saving" @click="restore(revision)">
                  Restore
                </Button>
              </template>
            </DataListItem>
          </DataList>
        </div>
      </SurfaceCard>
    </div>
  </PageBody>
</template>
