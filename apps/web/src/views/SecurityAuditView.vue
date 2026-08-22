<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { securityAuditApi, type SecurityAuditEvent } from '@/api/securityAuditApi'
import { Button, DataTable, PageBody, PageHeader, StatusMessage } from '@/ui'

const events = ref<SecurityAuditEvent[]>([])
const pageNumber = ref(1)
const hasMore = ref(false)
const loading = ref(false)
const error = ref<string | null>(null)

function formatDate(value: string): string {
  const date = new Date(value)
  return Number.isNaN(date.getTime()) ? value : date.toLocaleString()
}

async function load(page = 1) {
  loading.value = true
  error.value = null
  try {
    const result = await securityAuditApi.list(page)
    events.value = page > 1 ? [...events.value, ...result.items] : result.items
    pageNumber.value = result.page
    hasMore.value = result.page * result.pageSize < result.total
  } catch (err) {
    error.value = err instanceof Error ? err.message : 'Failed to load the security audit log'
  } finally {
    loading.value = false
  }
}

onMounted(() => load())
</script>

<template>
  <PageBody>
    <PageHeader
      eyebrow="Settings"
      title="Security audit"
      description="Review authentication and administrative security events."
    />

    <StatusMessage v-if="error" tone="error">{{ error }}</StatusMessage>
    <StatusMessage v-if="loading && events.length === 0">Loading…</StatusMessage>

    <DataTable v-else>
      <thead class="border-b bg-muted/50">
        <tr>
          <th scope="col" class="px-4 py-3 font-medium">Time</th>
          <th scope="col" class="px-4 py-3 font-medium">Event</th>
          <th scope="col" class="px-4 py-3 font-medium">Actor</th>
          <th scope="col" class="px-4 py-3 font-medium">Target</th>
          <th scope="col" class="px-4 py-3 font-medium">Outcome</th>
        </tr>
      </thead>
      <tbody class="divide-y">
        <tr v-for="event in events" :key="event.id">
          <td class="whitespace-nowrap px-4 py-3">{{ formatDate(event.occurredAt) }}</td>
          <td class="px-4 py-3">{{ event.eventType }}</td>
          <td class="px-4 py-3">{{ event.actorEmail || 'System' }}</td>
          <td class="px-4 py-3">{{ event.subjectId || '—' }}</td>
          <td class="px-4 py-3">{{ event.outcome }}</td>
        </tr>
        <tr v-if="events.length === 0">
          <td colspan="5" class="px-4 py-8 text-center text-muted-foreground">
            No security events found.
          </td>
        </tr>
      </tbody>
    </DataTable>

    <Button
      v-if="hasMore"
      type="button"
      variant="outline"
      :disabled="loading"
      @click="load(pageNumber + 1)"
    >
      {{ loading ? 'Loading…' : 'Load more' }}
    </Button>
  </PageBody>
</template>
