<script setup lang="ts">
import { computed, inject, type Ref } from 'vue'
import { cn } from '@/lib/utils'

const flush = inject<Ref<boolean>>(
  'dataListFlush',
  computed(() => false),
)
</script>

<template>
  <li :class="cn('text-center text-sm text-muted-foreground', flush ? 'px-3 py-6' : 'p-6')">
    <slot />
  </li>
</template>
