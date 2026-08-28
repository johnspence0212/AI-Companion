<script setup lang="ts">
import { ChevronDown, ChevronRight, Plus } from 'lucide-vue-next'
import type { NotesTreeNode } from '@/lib/notesTree'
import { cn } from '@/lib/utils'
import { Button } from '@/ui/button'

defineOptions({ name: 'NotesTree' })

defineProps<{
  items: NotesTreeNode[]
  selectedId?: string | null
  expandedIds: string[]
  depth?: number
  pending?: boolean
}>()

const emit = defineEmits<{
  select: [id: string]
  addChild: [parentId: string]
  toggle: [id: string]
}>()

const pads = ['pl-1', 'pl-4', 'pl-7', 'pl-10', 'pl-12'] as const

function padClass(depth: number) {
  return pads[Math.min(Math.max(depth, 0), pads.length - 1)]
}

function isExpanded(expandedIds: string[], id: string) {
  return expandedIds.includes(id)
}
</script>

<template>
  <ul :role="(depth ?? 0) === 0 ? 'tree' : 'group'" class="min-w-0">
    <li v-for="item in items" :key="item.id" role="treeitem">
      <div
        :class="
          cn(
            'flex items-center gap-1 border-b py-1 pr-2',
            padClass(depth ?? 0),
            selectedId === item.id && 'bg-accent',
          )
        "
      >
        <Button
          v-if="item.children.length > 0"
          type="button"
          variant="ghost"
          size="icon-sm"
          class="shrink-0"
          :aria-label="isExpanded(expandedIds, item.id) ? 'Collapse' : 'Expand'"
          :aria-expanded="isExpanded(expandedIds, item.id)"
          :disabled="pending"
          @click="emit('toggle', item.id)"
        >
          <ChevronDown v-if="isExpanded(expandedIds, item.id)" />
          <ChevronRight v-else />
        </Button>
        <span v-else class="size-8 shrink-0" aria-hidden="true" />
        <button
          type="button"
          class="min-w-0 flex-1 truncate rounded-md px-1 py-1.5 text-left text-sm font-medium hover:bg-muted/40"
          :aria-current="selectedId === item.id ? 'true' : undefined"
          :disabled="pending"
          @click="emit('select', item.id)"
        >
          {{ item.title }}
        </button>
        <Button
          type="button"
          variant="ghost"
          size="icon-sm"
          class="shrink-0"
          title="Add a note inside"
          aria-label="Add a note inside"
          :disabled="pending"
          @click="emit('addChild', item.id)"
        >
          <Plus />
        </Button>
      </div>
      <NotesTree
        v-if="item.children.length > 0 && isExpanded(expandedIds, item.id)"
        :items="item.children"
        :selected-id="selectedId"
        :expanded-ids="expandedIds"
        :depth="(depth ?? 0) + 1"
        :pending="pending"
        @select="emit('select', $event)"
        @add-child="emit('addChild', $event)"
        @toggle="emit('toggle', $event)"
      />
    </li>
  </ul>
</template>
