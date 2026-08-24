<script setup lang="ts">
defineProps<{
  title: string
}>()
</script>

<template>
  <section>
    <h3
      class="sticky top-0 z-10 border-b bg-background px-3 py-2 text-xs font-medium tracking-wide text-muted-foreground uppercase"
    >
      {{ title }}
    </h3>
    <slot />
  </section>
</template>
