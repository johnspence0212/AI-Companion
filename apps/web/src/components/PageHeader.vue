<script setup lang="ts">
import { cn } from '@/lib/utils'

withDefaults(
  defineProps<{
    title: string
    description?: string
    /** Optional module/section label shown above the title (e.g. Settings). */
    eyebrow?: string
    /** Compact toolbar for workbench destinations; default is the admin page hero. */
    size?: 'default' | 'compact'
  }>(),
  {
    size: 'default',
  },
)
</script>

<template>
  <div
    :class="
      cn(
        'flex shrink-0 items-start justify-between gap-4',
        size === 'compact' ? 'border-b px-4 py-3' : 'flex-wrap',
      )
    "
  >
    <div class="min-w-0">
      <p
        v-if="eyebrow"
        class="mb-1 text-xs font-semibold tracking-[0.14em] text-muted-foreground uppercase"
      >
        {{ eyebrow }}
      </p>
      <h1
        :class="
          size === 'compact'
            ? 'text-sm font-semibold tracking-tight'
            : 'mb-1 text-3xl font-bold tracking-tight'
        "
      >
        {{ title }}
      </h1>
      <p
        v-if="description"
        :class="size === 'compact' ? 'text-xs text-muted-foreground' : 'text-muted-foreground'"
      >
        {{ description }}
      </p>
    </div>
    <div v-if="$slots.actions" class="flex shrink-0 items-center gap-2">
      <slot name="actions" />
    </div>
  </div>
</template>
