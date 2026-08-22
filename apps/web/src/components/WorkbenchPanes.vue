<script setup lang="ts">
import SurfaceCard from './SurfaceCard.vue'

defineProps<{
  systemView?: string
  navTitle?: string
  listTitle?: string
  detailTitle?: string
}>()
</script>

<template>
  <div class="space-y-4">
    <p v-if="systemView" class="text-sm text-muted-foreground">View: {{ systemView }}</p>
    <div class="grid gap-6 lg:grid-cols-3">
      <SurfaceCard class="min-w-0">
        <h2 v-if="navTitle" class="mb-4 font-semibold">{{ navTitle }}</h2>
        <slot name="nav" />
      </SurfaceCard>
      <SurfaceCard class="min-w-0">
        <h2 v-if="listTitle" class="mb-4 font-semibold">{{ listTitle }}</h2>
        <slot name="list" />
      </SurfaceCard>
      <SurfaceCard class="min-w-0">
        <h2 v-if="detailTitle" class="mb-4 font-semibold">{{ detailTitle }}</h2>
        <slot name="detail" />
      </SurfaceCard>
    </div>
  </div>
</template>
