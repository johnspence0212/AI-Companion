<script setup lang="ts">
import { PageBody, PageHeader, StatusMessage, SurfaceCard } from '@/ui'
import { useAuthStore } from '@/stores/auth'
import { appName } from '@/config'

const auth = useAuthStore()
</script>

<template>
  <PageBody>
    <PageHeader
      :title="`Welcome to ${appName}`"
      description="Your secure application shell is ready. Review your profile, then manage users and roles from Settings when you have access."
    />

    <div class="grid gap-4 md:grid-cols-2">
      <SurfaceCard to="/profile">
        <h2 class="font-semibold">Complete your profile</h2>
        <StatusMessage>Confirm your display name and account details.</StatusMessage>
      </SurfaceCard>
      <SurfaceCard>
        <h2 class="font-semibold">Signed in securely</h2>
        <StatusMessage>{{ auth.user?.email ?? 'Loading account…' }}</StatusMessage>
      </SurfaceCard>
    </div>
  </PageBody>
</template>
