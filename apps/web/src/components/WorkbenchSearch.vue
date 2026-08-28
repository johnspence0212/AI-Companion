<script setup lang="ts">
import { computed, nextTick, ref, watch } from 'vue'
import { Search } from 'lucide-vue-next'
import { documentsApi, type LibraryDocument } from '@/api/documentsApi'
import { issuesApi, type Issue } from '@/api/issuesApi'
import { projectsApi, type ProjectContext } from '@/api/projectsApi'
import { searchApi, type SearchHit, type SearchResults } from '@/api/searchApi'
import { useAuthStore } from '@/stores/auth'
import { Button } from '@/ui/button'
import { Dialog, DialogContent, DialogDescription, DialogHeader, DialogTitle } from '@/ui/dialog'
import { Input } from '@/ui/input'
import DataList from './DataList.vue'
import DataListEmpty from './DataListEmpty.vue'
import DataListItem from './DataListItem.vue'
import FormSection from './FormSection.vue'
import FormSlideout from './FormSlideout.vue'
import MarkdownSource from './MarkdownSource.vue'
import StatusMessage from './StatusMessage.vue'
import WorkbenchSection from './WorkbenchSection.vue'

const auth = useAuthStore()
const canSearch = computed(() => auth.hasPermission('search.read'))

const query = ref('')
const results = ref<SearchResults | null>(null)
const loading = ref(false)
const error = ref<string | null>(null)
const dialogOpen = ref(false)
const dialogInput = ref<{ $el?: HTMLInputElement } | null>(null)

const slideoutOpen = ref(false)
const slideoutTitle = ref('Search result')
const openedDocument = ref<LibraryDocument | null>(null)
const openedIssue = ref<Issue | null>(null)
const openedContext = ref<ProjectContext | null>(null)
const openedActivity = ref<SearchHit | null>(null)

const groups = computed(() => [
  { key: 'projects', label: 'Projects', items: results.value?.projects ?? [] },
  { key: 'documents', label: 'Documents', items: results.value?.documents ?? [] },
  { key: 'issues', label: 'Issues', items: results.value?.issues ?? [] },
  { key: 'activity', label: 'Activity', items: results.value?.activity ?? [] },
])

const visibleGroups = computed(() => groups.value.filter((group) => group.items.length > 0))

watch(dialogOpen, async (open) => {
  if (!open) {
    return
  }

  await nextTick()
  const input = dialogInput.value?.$el ?? dialogInput.value
  if (input instanceof HTMLInputElement) {
    input.focus()
    input.select()
  }
})

function when(value: string) {
  const parsed = new Date(value)
  return Number.isNaN(parsed.getTime()) ? value : parsed.toLocaleString()
}

function resetOpened() {
  openedDocument.value = null
  openedIssue.value = null
  openedContext.value = null
  openedActivity.value = null
}

async function runSearch() {
  if (!query.value.trim()) {
    dialogOpen.value = true
    return
  }

  dialogOpen.value = true
  loading.value = true
  error.value = null
  try {
    results.value = await searchApi.query(query.value.trim())
  } catch (e) {
    error.value = e instanceof Error ? e.message : 'Failed to search'
    throw e
  } finally {
    loading.value = false
  }
}

function openFromHeader() {
  dialogOpen.value = true
  if (query.value.trim() && !results.value) {
    void runSearch()
  }
}

async function openHit(kind: string, hit: SearchHit) {
  resetOpened()
  slideoutTitle.value = hit.title
  dialogOpen.value = false
  await nextTick()
  slideoutOpen.value = true
  error.value = null
  try {
    if (kind === 'documents') {
      openedDocument.value = await documentsApi.get(hit.id)
      slideoutTitle.value = openedDocument.value.title
      return
    }

    if (kind === 'issues') {
      openedIssue.value = await issuesApi.get(hit.id)
      slideoutTitle.value = openedIssue.value.title
      return
    }

    if (kind === 'projects') {
      openedContext.value = await projectsApi.getContext(hit.id)
      slideoutTitle.value = openedContext.value.title
      return
    }

    openedActivity.value = hit
  } catch (e) {
    error.value = e instanceof Error ? e.message : 'Failed to open search result'
    throw e
  }
}

function markdown() {
  return (
    openedDocument.value?.body ??
    openedIssue.value?.description ??
    openedContext.value?.body ??
    openedActivity.value?.title ??
    ''
  )
}
</script>

<template>
  <div v-if="canSearch" class="flex min-w-0 flex-1 items-center justify-end gap-2">
    <Button
      type="button"
      variant="ghost"
      size="icon"
      class="md:hidden"
      aria-label="Search"
      @click="openFromHeader"
    >
      <Search />
    </Button>
    <form class="relative hidden w-full max-w-sm md:block" @submit.prevent="runSearch">
      <Search
        class="pointer-events-none absolute top-1/2 left-2.5 size-4 -translate-y-1/2 text-muted-foreground"
      />
      <Input
        v-model="query"
        name="workbench-search"
        class="pl-8"
        placeholder="Search"
        autocomplete="off"
        aria-label="Search"
      />
    </form>

    <Dialog :open="dialogOpen" @update:open="dialogOpen = $event">
      <DialogContent
        class="flex max-h-[min(80vh,40rem)] w-[min(42rem,calc(100vw-2rem))] max-w-none flex-col overflow-hidden"
      >
        <DialogHeader>
          <DialogTitle>Search</DialogTitle>
          <DialogDescription>Projects, Documents, Issues, and Activity.</DialogDescription>
        </DialogHeader>
        <form class="px-6" @submit.prevent="runSearch">
          <Input
            ref="dialogInput"
            v-model="query"
            name="workbench-search-dialog"
            placeholder="Search"
            autocomplete="off"
            aria-label="Search query"
          />
        </form>
        <StatusMessage v-if="error" class="px-6" tone="error">{{ error }}</StatusMessage>
        <div class="min-h-0 flex-1 overflow-y-auto">
          <DataList v-if="loading" variant="flush">
            <DataListEmpty>Searching…</DataListEmpty>
          </DataList>
          <DataList v-else-if="!results" variant="flush">
            <DataListEmpty>Type a query to search the library and Projects.</DataListEmpty>
          </DataList>
          <DataList v-else-if="visibleGroups.length === 0" variant="flush">
            <DataListEmpty>No matches.</DataListEmpty>
          </DataList>
          <template v-else>
            <WorkbenchSection v-for="group in visibleGroups" :key="group.key" :title="group.label">
              <DataList variant="flush">
                <DataListItem
                  v-for="hit in group.items"
                  :key="hit.id"
                  :title="hit.title"
                  :description="when(hit.updatedAt)"
                  interactive
                  @click="openHit(group.key, hit)"
                />
              </DataList>
            </WorkbenchSection>
          </template>
        </div>
      </DialogContent>
    </Dialog>

    <FormSlideout
      :open="slideoutOpen"
      :title="slideoutTitle"
      description="Search opens the shared slide-out. Source Markdown is unchanged."
      :show-submit="false"
      cancel-label="Close"
      allow-fullscreen
      size="wide"
      @update:open="slideoutOpen = $event"
    >
      <FormSection title="Result">
        <StatusMessage v-if="error" tone="error">{{ error }}</StatusMessage>
        <StatusMessage v-if="openedIssue">
          {{ openedIssue.status }} · {{ openedIssue.priority }}
        </StatusMessage>
        <MarkdownSource :model-value="markdown()" readonly />
      </FormSection>
    </FormSlideout>
  </div>
</template>
