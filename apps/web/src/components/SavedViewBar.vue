<script setup lang="ts">
import { computed } from 'vue'
import { Button } from '@/ui/button'
import DataList from './DataList.vue'
import DataListEmpty from './DataListEmpty.vue'
import DataListItem from './DataListItem.vue'

const props = defineProps<{
  views: Array<{ id: string; name: string; isSystem: boolean }>
  selectedId: string | null
  pending?: boolean
}>()

const emit = defineEmits<{
  select: [id: string]
  duplicate: []
  edit: []
}>()

const selected = computed(() => props.views.find((view) => view.id === props.selectedId) ?? null)
</script>

<template>
  <div class="space-y-3">
    <DataList>
      <DataListEmpty v-if="views.length === 0">No Saved Views.</DataListEmpty>
      <DataListItem
        v-for="view in views"
        :key="view.id"
        :title="view.name"
        :description="view.isSystem ? 'System' : 'Yours'"
        interactive
        :selected="selectedId === view.id"
        @click="emit('select', view.id)"
      />
    </DataList>
    <div class="flex flex-wrap gap-2">
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
  </div>
</template>
