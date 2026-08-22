<script setup lang="ts">
defineProps<{
  title: string
}>()
</script>

<template>
  <section class="form-section flex flex-col gap-4">
    <h3 class="text-sm font-semibold tracking-tight text-foreground">{{ title }}</h3>
    <div class="flex flex-col gap-4">
      <slot />
    </div>
  </section>
</template>
