<script setup lang="ts">
import { computed, ref } from 'vue'
import { documentsApi, type LibraryDocument } from '@/api/documentsApi'
import { issuesApi, type Issue } from '@/api/issuesApi'
import { projectsApi, type ProjectContext } from '@/api/projectsApi'
import { searchApi, type SearchHit, type SearchResults } from '@/api/searchApi'
import {
  DataList,
  DataListEmpty,
  DataListItem,
  FormSection,
  FormSlideout,
  MarkdownSource,
  PageBody,
  PageHeader,
  StatusMessage,
  WorkbenchComposer,
  WorkbenchPanes,
  WorkbenchSection,
} from '@/ui'

const query = ref('')
const results = ref<SearchResults | null>(null)
const loading = ref(false)
const error = ref<string | null>(null)
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

const hasResults = computed(() => groups.value.some((group) => group.items.length > 0))

function resetOpened() {
  openedDocument.value = null
  openedIssue.value = null
  openedContext.value = null
  openedActivity.value = null
}

async function runSearch() {
  if (!query.value.trim()) {
    return
  }

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

async function openHit(kind: string, hit: SearchHit) {
  resetOpened()
  slideoutTitle.value = hit.title
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
  <PageBody variant="workbench">
    <PageHeader
      size="compact"
      title="Search"
      description="Projects, Documents, Issues, and Activity."
    />

    <StatusMessage v-if="error" class="px-4 py-2" tone="error">{{ error }}</StatusMessage>

    <WorkbenchPanes list-title="Results">
      <template #list-toolbar>
        <WorkbenchComposer
          v-model="query"
          name="search"
          placeholder="Search"
          submit-label="Search"
          :pending="loading"
          @submit="runSearch"
        />
      </template>
      <template #list>
        <DataList v-if="!results && !loading" variant="flush">
          <DataListEmpty>Type a query to search the library and Projects.</DataListEmpty>
        </DataList>
        <DataList v-else-if="results && !hasResults" variant="flush">
          <DataListEmpty>No matches.</DataListEmpty>
        </DataList>
        <template v-else>
          <WorkbenchSection v-for="group in groups" :key="group.key" :title="group.label">
            <DataList variant="flush">
              <DataListEmpty v-if="!loading && group.items.length === 0">No matches.</DataListEmpty>
              <DataListItem
                v-for="hit in group.items"
                :key="hit.id"
                :title="hit.title"
                :description="hit.updatedAt"
                interactive
                @click="openHit(group.key, hit)"
              />
            </DataList>
          </WorkbenchSection>
        </template>
      </template>
    </WorkbenchPanes>

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
        <StatusMessage v-if="openedIssue">
          {{ openedIssue.status }} · {{ openedIssue.priority }}
        </StatusMessage>
        <MarkdownSource :model-value="markdown()" readonly />
      </FormSection>
    </FormSlideout>
  </PageBody>
</template>
