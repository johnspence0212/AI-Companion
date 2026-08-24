<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { dailyApi, localDateOnly, type Daily, type DailyItem } from '@/api/dailyApi'
import {
  Button,
  DataList,
  DataListEmpty,
  DataListItem,
  PageBody,
  StatusMessage,
  WorkbenchComposer,
  WorkbenchPanes,
  WorkbenchSection,
} from '@/ui'

const date = localDateOnly()
const daily = ref<Daily | null>(null)
const draft = ref('')
const loading = ref(false)
const saving = ref(false)
const error = ref<string | null>(null)

const heading = computed(() =>
  new Date(`${date}T00:00:00`).toLocaleDateString(undefined, {
    weekday: 'long',
    month: 'long',
    day: 'numeric',
  }),
)

function label(item: DailyItem) {
  return item.customText ?? item.issueTitle ?? 'Daily Item'
}

function when(value: string) {
  return new Date(`${value}T00:00:00`).toLocaleDateString(undefined, {
    weekday: 'short',
    month: 'short',
    day: 'numeric',
  })
}

function detail(item: DailyItem) {
  if (item.issueTitle) {
    return item.issueStatus ? `Issue · ${item.issueStatus}` : 'Issue'
  }

  return 'Custom Daily Item'
}

async function load() {
  loading.value = true
  error.value = null
  try {
    daily.value = await dailyApi.get(date)
  } catch (e) {
    error.value = e instanceof Error ? e.message : 'Failed to load Today'
    throw e
  } finally {
    loading.value = false
  }
}

async function addItem() {
  if (!draft.value.trim()) {
    return
  }

  saving.value = true
  error.value = null
  try {
    await dailyApi.addItem(date, draft.value.trim())
    draft.value = ''
    daily.value = await dailyApi.get(date)
  } catch (e) {
    error.value = e instanceof Error ? e.message : 'Failed to add Daily Item'
    throw e
  } finally {
    saving.value = false
  }
}

async function complete(item: DailyItem) {
  saving.value = true
  error.value = null
  try {
    await dailyApi.complete(item.id)
    daily.value = await dailyApi.get(date)
  } catch (e) {
    error.value = e instanceof Error ? e.message : 'Failed to complete Daily Item'
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
  <PageBody variant="workbench">
    <StatusMessage v-if="error" class="px-4 py-2" tone="error">{{ error }}</StatusMessage>
    <StatusMessage v-else-if="loading" class="px-4 py-2">Loading Daily…</StatusMessage>

    <WorkbenchPanes layout="home" :list-title="heading" detail-title="Waiting">
      <template #list-toolbar>
        <WorkbenchComposer
          v-model="draft"
          name="daily-item"
          placeholder="Add a custom Daily Item"
          submit-label="Add"
          :pending="saving"
          @submit="addItem"
        />
      </template>
      <template #list>
        <DataList variant="flush">
          <DataListEmpty v-if="!loading && (daily?.items.length ?? 0) === 0">
            Nothing on Today yet.
          </DataListEmpty>
          <DataListItem
            v-for="item in daily?.items ?? []"
            :key="item.id"
            :title="label(item)"
            :description="item.completedAt ? `${detail(item)} · done` : detail(item)"
          >
            <template v-if="!item.completedAt" #actions>
              <Button
                size="sm"
                variant="outline"
                shape="square"
                :disabled="saving"
                @click="complete(item)"
              >
                Complete
              </Button>
            </template>
          </DataListItem>
        </DataList>
      </template>
      <template #detail>
        <WorkbenchSection title="Carryover">
          <DataList variant="flush">
            <DataListEmpty v-if="!daily?.carryover.length">
              Incomplete items from the last 7 days stay on their original dates.
            </DataListEmpty>
            <DataListItem
              v-for="item in daily?.carryover ?? []"
              :key="item.id"
              :title="label(item)"
              :description="when(item.date)"
            />
          </DataList>
        </WorkbenchSection>
        <WorkbenchSection title="Blocked / Waiting">
          <DataList variant="flush">
            <DataListEmpty v-if="!loading && (daily?.blocked.length ?? 0) === 0">
              Nothing blocked.
            </DataListEmpty>
            <DataListItem
              v-for="issue in daily?.blocked ?? []"
              :key="issue.issueId"
              :title="issue.title"
              :description="issue.blockedReason ?? issue.status"
            />
          </DataList>
        </WorkbenchSection>
      </template>
    </WorkbenchPanes>
  </PageBody>
</template>
