<script setup lang="ts">
import { computed, useAttrs } from 'vue'
import { cn } from '@/lib/utils'

const props = withDefaults(
  defineProps<{
    /** Render as a form (auth/profile) or a surface panel. */
    as?: 'form' | 'div'
    /** Max width of the panel. */
    size?: 'sm' | 'md' | 'full'
  }>(),
  {
    as: 'div',
    size: 'sm',
  },
)

defineOptions({ inheritAttrs: false })

const attrs = useAttrs()

const sizeClass = computed(() => {
  switch (props.size) {
    case 'md':
      return 'max-w-xl'
    case 'full':
      return 'max-w-none'
    default:
      return 'max-w-sm'
  }
})

const panelClass = computed(() =>
  cn(
    'w-full space-y-4 rounded-lg border bg-card p-6 shadow-sm',
    sizeClass.value,
    attrs.class as string | undefined,
  ),
)

const boundAttrs = computed(() => {
  const rest = { ...attrs }
  delete rest.class
  return rest
})
</script>

<template>
  <component :is="as" :class="panelClass" v-bind="boundAttrs">
    <slot />
  </component>
</template>
