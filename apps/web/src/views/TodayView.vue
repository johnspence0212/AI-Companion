<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { dailyApi, localDateOnly, type Daily, type DailyItem } from '@/api/dailyApi'
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

const carryoverByDate = computed(() => {
  const groups = new Map<string, DailyItem[]>()
  for (const item of daily.value?.carryover ?? []) {
    const bucket = groups.get(item.date) ?? []
    bucket.push(item)
    groups.set(item.date, bucket)
  }

  return [...groups.entries()]
})

function label(item: DailyItem) {
  return item.customText ?? item.issueTitle ?? 'Daily Item'
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
  <PageBody>
    <PageHeader title="Today" :description="heading" />

    <StatusMessage v-if="error" tone="error">{{ error }}</StatusMessage>
    <StatusMessage v-else-if="loading">Loading Daily…</StatusMessage>

    <SurfaceCard>
      <h2 class="font-semibold">Daily</h2>
      <form class="mt-4" @submit.prevent="addItem">
        <FormField label="Add a custom Daily Item">
          <Input v-model="draft" name="daily-item" autocomplete="off" />
        </FormField>
        <Button class="mt-3" type="submit" :disabled="saving || !draft.trim()">Add</Button>
      </form>
      <DataList class="mt-4">
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
            <Button size="sm" variant="outline" :disabled="saving" @click="complete(item)">
              Complete
            </Button>
          </template>
        </DataListItem>
      </DataList>
    </SurfaceCard>

    <SurfaceCard>
      <h2 class="font-semibold">Carryover</h2>
      <StatusMessage>
        Incomplete items from the last 7 days stay on their original dates. They are not auto-moved.
      </StatusMessage>
      <DataList class="mt-4">
        <DataListEmpty v-if="!carryoverByDate.length">No carryover.</DataListEmpty>
        <DataListItem
          v-for="item in daily?.carryover ?? []"
          :key="item.id"
          :title="label(item)"
          :description="item.date"
        />
      </DataList>
    </SurfaceCard>

    <SurfaceCard>
      <h2 class="font-semibold">Blocked / Waiting</h2>
      <DataList>
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
    </SurfaceCard>
  </PageBody>
</template>
