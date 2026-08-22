<script setup lang="ts">
import { computed } from 'vue'
import { cn } from '@/lib/utils'

const props = withDefaults(
  defineProps<{
    /** Visual tone — maps to semantic tokens only. */
    tone?: 'error' | 'muted' | 'success'
    /** Accessible role when announcing errors/status. */
    role?: 'alert' | 'status' | undefined
  }>(),
  {
    tone: 'muted',
  },
)

const toneClass = computed(() => {
  switch (props.tone) {
    case 'error':
      return 'text-destructive'
    case 'success':
      return 'text-primary'
    default:
      return 'text-muted-foreground'
  }
})

const resolvedRole = computed(() => {
  if (props.role) return props.role
  if (props.tone === 'error') return 'alert'
  return undefined
})
</script>

<template>
  <p :role="resolvedRole" :class="cn('text-sm', toneClass)">
    <slot />
  </p>
</template>
