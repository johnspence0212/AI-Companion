<script setup lang="ts">
import { computed, provide } from 'vue'
import { cn } from '@/lib/utils'

const props = withDefaults(
  defineProps<{
    /** `panel` is the bordered admin list. `flush` sits inside a workbench pane. */
    variant?: 'panel' | 'flush'
  }>(),
  {
    variant: 'panel',
  },
)

const flush = computed(() => props.variant === 'flush')
provide('dataListFlush', flush)
</script>

<template>
  <ul :class="cn('divide-y', variant === 'panel' && 'rounded-lg border bg-card')">
    <slot />
  </ul>
</template>
