<script setup lang="ts">
import { computed } from 'vue'
import { cn } from '@/lib/utils'

const props = withDefaults(
  defineProps<{
    /** When set, renders as a router-link. */
    to?: string
  }>(),
  {},
)

const tag = computed(() => (props.to ? 'router-link' : 'div'))
</script>

<template>
  <component
    :is="tag"
    :to="to"
    :class="cn('rounded-lg border bg-card p-5', to && 'transition-colors hover:bg-muted/40')"
  >
    <slot />
  </component>
</template>
