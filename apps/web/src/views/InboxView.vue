<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { documentsApi } from '@/api/documentsApi'
import { inboxApi, type InboxItem } from '@/api/inboxApi'
import { projectsApi, type Project } from '@/api/projectsApi'
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
  StatusMessage,
  SurfaceCard,
} from '@/ui'

const items = ref<InboxItem[]>([])
const projects = ref<Project[]>([])
const selected = ref<InboxItem | null>(null)
const draft = ref('')
const title = ref('')
const projectId = ref('')
const target = ref<'document' | 'issue'>('document')
const slideoutOpen = ref(false)
const loading = ref(false)
const saving = ref(false)
const error = ref<string | null>(null)
const notice = ref<string | null>(null)

const preview = computed(() => selected.value?.text ?? '')

async function load() {
  loading.value = true
  error.value = null
  try {
    const [inboxItems, projectItems] = await Promise.all([inboxApi.list(), projectsApi.list()])
    items.value = inboxItems
    projects.value = projectItems
    if (!projectId.value && projectItems[0]) {
      projectId.value = projectItems[0].id
    }
  } catch (e) {
    error.value = e instanceof Error ? e.message : 'Failed to load Inbox'
    throw e
  } finally {
    loading.value = false
  }
}

function open(item: InboxItem) {
  selected.value = item
  title.value = item.text.split('\n')[0]?.slice(0, 500) ?? ''
  target.value = 'document'
  slideoutOpen.value = true
}

async function capture() {
  if (!draft.value.trim()) {
    return
  }

  saving.value = true
  error.value = null
  try {
    const created = await inboxApi.capture(draft.value.trim())
    draft.value = ''
    await load()
    open(created)
    notice.value = 'Captured.'
  } catch (e) {
    error.value = e instanceof Error ? e.message : 'Failed to capture Inbox Item'
    throw e
  } finally {
    saving.value = false
  }
}

async function processItem() {
  if (!selected.value) {
    return
  }

  saving.value = true
  error.value = null
  try {
    const processed =
      target.value === 'document'
        ? await inboxApi.process(selected.value.id, { createDocument: true, title: title.value })
        : await inboxApi.process(selected.value.id, {
            createIssue: true,
            title: title.value,
            projectId: projectId.value,
          })
    if (processed.documentId) {
      const document = await documentsApi.get(processed.documentId)
      title.value = document.title
    }

    slideoutOpen.value = false
    await load()
    notice.value =
      target.value === 'document' ? 'Processed to a Document.' : 'Processed to an Issue.'
  } catch (e) {
    error.value = e instanceof Error ? e.message : 'Failed to process Inbox Item'
    throw e
  } finally {
    saving.value = false
  }
}

async function archiveItem() {
  if (!selected.value) {
    return
  }

  saving.value = true
  error.value = null
  try {
    await inboxApi.archive(selected.value.id)
    slideoutOpen.value = false
    selected.value = null
    await load()
    notice.value = 'Archived without a target.'
  } catch (e) {
    error.value = e instanceof Error ? e.message : 'Failed to archive Inbox Item'
    throw e
  } finally {
    saving.value = false
  }
}

onMounted(() => {
  void load()
})
</script>

<template>
  <PageBody>
    <PageHeader
      title="Inbox"
      description="Capture unclassified thoughts, then process them to a Document or Issue."
    />

    <StatusMessage v-if="error" tone="error">{{ error }}</StatusMessage>
    <StatusMessage v-else-if="notice" tone="success">{{ notice }}</StatusMessage>

    <SurfaceCard>
      <form @submit.prevent="capture">
        <FormField label="Capture">
          <Input v-model="draft" name="inbox-capture" autocomplete="off" />
        </FormField>
        <Button class="mt-3" type="submit" shape="square" :disabled="saving || !draft.trim()">
          Capture
        </Button>
      </form>
      <DataList class="mt-4">
        <DataListEmpty v-if="!loading && items.length === 0">Inbox is empty.</DataListEmpty>
        <DataListItem
          v-for="item in items"
          :key="item.id"
          :title="item.text.split('\n')[0] || 'Inbox Item'"
          :description="item.status"
          interactive
          :selected="selected?.id === item.id"
          @click="open(item)"
        />
      </DataList>
    </SurfaceCard>

    <FormSlideout
      :open="slideoutOpen"
      title="Process Inbox Item"
      description="Create a Document or Issue, or archive without a target."
      submit-label="Process"
      :pending="saving"
      allow-fullscreen
      size="wide"
      @update:open="slideoutOpen = $event"
      @submit="processItem"
    >
      <FormSection title="Item">
        <MarkdownSource :model-value="preview" label="Captured text" readonly />
        <FormField label="Title">
          <Input v-model="title" name="inbox-title" autocomplete="off" />
        </FormField>
        <FormField label="Process as">
          <select v-model="target">
            <option value="document">New Document</option>
            <option value="issue">New Issue</option>
          </select>
        </FormField>
        <FormField v-if="target === 'issue'" label="Project">
          <select v-model="projectId">
            <option v-for="project in projects" :key="project.id" :value="project.id">
              {{ project.name }}
            </option>
          </select>
        </FormField>
        <Button
          type="button"
          variant="outline"
          shape="square"
          :disabled="saving"
          @click="archiveItem"
        >
          Archive
        </Button>
      </FormSection>
    </FormSlideout>
  </PageBody>
</template>
