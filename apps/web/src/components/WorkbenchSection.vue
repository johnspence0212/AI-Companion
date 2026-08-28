<script setup lang="ts">
withDefaults(
  defineProps<{
    title: string
    collapsible?: boolean
  }>(),
  {
    collapsible: false,
  },
)
</script>

<template>
  <section v-if="collapsible" class="border-t">
    <details>
      <summary
        class="cursor-pointer px-5 py-3 text-xs font-medium tracking-wide text-muted-foreground uppercase"
      >
        {{ title }}
      </summary>
      <div>
        <slot />
      </div>
    </details>
  </section>
  <section v-else>
    <h3
      class="sticky top-0 z-10 border-b bg-background px-3 py-2 text-xs font-medium tracking-wide text-muted-foreground uppercase"
    >
      {{ title }}
    </h3>
    <slot />
  </section>
</template>
