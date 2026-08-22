<script setup lang="ts">
/**
 * Bordered, divided list panel for admin/index screens.
 * Pair with DataListItem / DataListEmpty — do not restyle in views.
 */
</script>

<template>
  <ul class="divide-y rounded-lg border bg-card">
    <slot />
  </ul>
</template>
