<script setup lang="ts">
import { computed, nextTick, ref, watch } from 'vue'
import { Inbox } from 'lucide-vue-next'
import { documentsApi } from '@/api/documentsApi'
import { inboxApi, type InboxItem } from '@/api/inboxApi'
import { projectsApi, type Project } from '@/api/projectsApi'
import { useAuthStore } from '@/stores/auth'
import { Button } from '@/ui/button'
import { Dialog, DialogContent, DialogDescription, DialogHeader, DialogTitle } from '@/ui/dialog'
import { Input } from '@/ui/input'
import DataList from './DataList.vue'
import DataListEmpty from './DataListEmpty.vue'
import DataListItem from './DataListItem.vue'
import FormField from './FormField.vue'
import FormSection from './FormSection.vue'
import FormSlideout from './FormSlideout.vue'
import MarkdownSource from './MarkdownSource.vue'
import StatusMessage from './StatusMessage.vue'
import WorkbenchComposer from './WorkbenchComposer.vue'

const auth = useAuthStore()
const canInbox = computed(() => auth.hasPermission('inbox.read'))
const canManage = computed(() => auth.hasPermission('inbox.manage'))

const items = ref<InboxItem[]>([])
const projects = ref<Project[]>([])
const selected = ref<InboxItem | null>(null)
const draft = ref('')
const title = ref('')
const projectId = ref('')
const target = ref<'document' | 'issue'>('document')
const dialogOpen = ref(false)
const slideoutOpen = ref(false)
const loading = ref(false)
const saving = ref(false)
const error = ref<string | null>(null)
const overlayNotice = ref<string | null>(null)
const processNotice = ref<string | null>(null)

const preview = computed(() => selected.value?.text ?? '')

watch(dialogOpen, (open) => {
  if (open) {
    void load()
  }
})

async function returnToInbox() {
  slideoutOpen.value = false
  await nextTick()
  dialogOpen.value = true
}

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

function openFromHeader() {
  overlayNotice.value = null
  dialogOpen.value = true
}

async function open(item: InboxItem) {
  selected.value = item
  title.value = item.text.split('\n')[0]?.slice(0, 500) ?? ''
  target.value = 'document'
  dialogOpen.value = false
  await nextTick()
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
    processNotice.value = 'Captured.'
    overlayNotice.value = null
    await open(created)
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

    processNotice.value = null
    overlayNotice.value =
      target.value === 'document' ? 'Processed to a Document.' : 'Processed to an Issue.'
    await load()
    await returnToInbox()
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
    processNotice.value = null
    overlayNotice.value = 'Archived without a target.'
    selected.value = null
    await load()
    await returnToInbox()
  } catch (e) {
    error.value = e instanceof Error ? e.message : 'Failed to archive Inbox Item'
    throw e
  } finally {
    saving.value = false
  }
}
</script>

<template>
  <div v-if="canInbox" class="flex items-center">
    <Button type="button" size="icon" aria-label="Inbox" @click="openFromHeader">
      <Inbox />
    </Button>

    <Dialog :open="dialogOpen" @update:open="dialogOpen = $event">
      <DialogContent
        class="flex max-h-[min(80vh,40rem)] w-[min(42rem,calc(100vw-2rem))] max-w-none flex-col overflow-hidden"
      >
        <DialogHeader>
          <DialogTitle>Inbox</DialogTitle>
          <DialogDescription
            >Capture a thought, then process it to a Document or Issue.</DialogDescription
          >
        </DialogHeader>
        <div v-if="canManage" class="px-6">
          <WorkbenchComposer
            v-model="draft"
            name="inbox-capture"
            placeholder="Capture a thought"
            submit-label="Capture"
            variant="plain"
            :pending="saving"
            @submit="capture"
          />
        </div>
        <StatusMessage v-if="error" class="px-6" tone="error">{{ error }}</StatusMessage>
        <StatusMessage v-else-if="overlayNotice" class="px-6" tone="success">{{
          overlayNotice
        }}</StatusMessage>
        <div class="min-h-0 flex-1 overflow-y-auto">
          <DataList variant="flush">
            <DataListEmpty v-if="loading">Loading…</DataListEmpty>
            <DataListEmpty v-else-if="items.length === 0">Inbox is empty.</DataListEmpty>
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
        </div>
      </DialogContent>
    </Dialog>

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
        <StatusMessage v-if="error" tone="error">{{ error }}</StatusMessage>
        <StatusMessage v-else-if="processNotice">{{ processNotice }}</StatusMessage>
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
  </div>
</template>
