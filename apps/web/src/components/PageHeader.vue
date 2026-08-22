<script setup lang="ts">
defineProps<{
  title: string
  description?: string
  /** Optional module/section label shown above the title (e.g. Settings). */
  eyebrow?: string
}>()
</script>

<template>
  <div class="flex flex-wrap items-start justify-between gap-4">
    <div class="min-w-0">
      <p
        v-if="eyebrow"
        class="mb-1 text-xs font-semibold tracking-[0.14em] text-muted-foreground uppercase"
      >
        {{ eyebrow }}
      </p>
      <h1 class="mb-1 text-3xl font-bold tracking-tight">{{ title }}</h1>
      <p v-if="description" class="text-muted-foreground">{{ description }}</p>
    </div>
    <div v-if="$slots.actions" class="flex shrink-0 items-center gap-2">
      <slot name="actions" />
    </div>
  </div>
</template>
