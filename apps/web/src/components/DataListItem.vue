<script setup lang="ts">
import { computed, inject, type Ref } from 'vue'
import { cn } from '@/lib/utils'

const props = defineProps<{
  title?: string
  description?: string
  selected?: boolean
  interactive?: boolean
}>()

const flush = inject<Ref<boolean>>(
  'dataListFlush',
  computed(() => false),
)
</script>

<template>
  <li
    :class="
      cn(
        'flex flex-wrap items-center justify-between gap-3',
        flush ? 'px-3 py-2' : 'p-4',
        props.interactive && 'cursor-pointer hover:bg-muted/40',
        props.selected && 'bg-accent',
      )
    "
  >
    <div class="min-w-0">
      <p v-if="title || $slots.title" class="truncate font-medium">
        <slot name="title">{{ title }}</slot>
      </p>
      <p v-if="description || $slots.description" class="truncate text-sm text-muted-foreground">
        <slot name="description">{{ description }}</slot>
      </p>
      <slot />
    </div>
    <div v-if="$slots.actions" class="flex shrink-0 items-start gap-2">
      <slot name="actions" />
    </div>
  </li>
</template>
