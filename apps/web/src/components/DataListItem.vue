<script setup lang="ts">
defineProps<{
  title?: string
  description?: string
}>()
</script>

<template>
  <li class="flex flex-wrap items-center justify-between gap-3 p-4">
    <div class="min-w-0">
      <p v-if="title || $slots.title" class="font-medium">
        <slot name="title">{{ title }}</slot>
      </p>
      <p v-if="description || $slots.description" class="text-sm text-muted-foreground">
        <slot name="description">{{ description }}</slot>
      </p>
      <slot />
    </div>
    <div v-if="$slots.actions" class="flex shrink-0 items-start gap-2">
      <slot name="actions" />
    </div>
  </li>
</template>
