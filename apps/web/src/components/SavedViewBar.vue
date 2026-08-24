<script setup lang="ts">
import { computed } from 'vue'
import { Button } from '@/ui/button'

const props = withDefaults(
  defineProps<{
    views: Array<{ id: string; name: string; isSystem: boolean }>
    selectedId: string | null
    pending?: boolean
    inputId?: string
  }>(),
  {
    inputId: 'saved-view',
  },
)

const emit = defineEmits<{
  select: [id: string]
  duplicate: []
  edit: []
}>()

const selected = computed(() => props.views.find((view) => view.id === props.selectedId) ?? null)

function onSelect(event: Event) {
  const value = (event.target as HTMLSelectElement).value
  if (value) {
    emit('select', value)
  }
}
</script>

<template>
  <div class="flex flex-wrap items-center gap-2 border-b px-3 py-2">
    <label class="sr-only" :for="inputId">Saved View</label>
    <select
      :id="inputId"
      class="h-8 min-w-0 flex-1 rounded-md border border-input bg-background px-2 text-sm"
      :value="selectedId ?? ''"
      :disabled="pending || views.length === 0"
      @change="onSelect"
    >
      <option v-if="views.length === 0" value="">No Saved Views</option>
      <option v-for="view in views" :key="view.id" :value="view.id">
        {{ view.isSystem ? view.name : `${view.name} (yours)` }}
      </option>
    </select>
    <Button
      type="button"
      size="sm"
      shape="square"
      :disabled="pending || !selected"
      @click="emit('duplicate')"
    >
      Duplicate
    </Button>
    <Button
      type="button"
      size="sm"
      variant="outline"
      shape="square"
      :disabled="pending || !selected || selected.isSystem"
      @click="emit('edit')"
    >
      Edit filters
    </Button>
  </div>
</template>
