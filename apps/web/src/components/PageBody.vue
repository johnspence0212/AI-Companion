<script setup lang="ts">
import { cn } from '@/lib/utils'

withDefaults(
  defineProps<{
    /**
     * `page` keeps padded admin screens. `workbench` fills the shell so panes can
     * scroll internally instead of stacking cards in a document flow.
     */
    variant?: 'page' | 'workbench'
  }>(),
  {
    variant: 'page',
  },
)
</script>

<template>
  <div
    :class="
      cn(
        variant === 'workbench'
          ? 'flex min-h-0 flex-1 flex-col overflow-hidden'
          : 'min-h-0 flex-1 space-y-6 overflow-y-auto p-8',
      )
    "
  >
    <slot />
  </div>
</template>
