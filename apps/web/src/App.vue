<script setup lang="ts">
import { computed } from 'vue'
import { useRoute } from 'vue-router'
import { AppShell, GuestShell } from '@/ui'

const route = useRoute()
const isStandaloneRoute = computed(
  () => route.meta.guest === true || route.name === 'change-password',
)
</script>

<template>
  <GuestShell v-if="isStandaloneRoute">
    <router-view />
  </GuestShell>
  <AppShell v-else>
    <router-view />
  </AppShell>
</template>
