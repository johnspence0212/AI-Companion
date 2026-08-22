<script setup lang="ts">
/**
 * Bordered table panel for dense admin data (e.g. security audit).
 */
</script>

<template>
  <div class="overflow-x-auto rounded-lg border bg-card">
    <table class="w-full min-w-3xl text-left text-sm">
      <slot />
    </table>
  </div>
</template>
