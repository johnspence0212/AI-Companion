<script setup lang="ts">
import { ref, watch } from 'vue'
import { useAuthStore } from '@/stores/auth'
import {
  Button,
  FormField,
  FormPanel,
  FormSection,
  Input,
  PageBody,
  PageHeader,
  StatusMessage,
} from '@/ui'

const auth = useAuthStore()
const displayName = ref(auth.user?.displayName ?? '')
const saved = ref(false)

watch(
  () => auth.user?.displayName,
  (value) => {
    displayName.value = value ?? ''
  },
)

async function saveProfile() {
  saved.value = false
  await auth.updateProfile(displayName.value)
  saved.value = true
}
</script>

<template>
  <PageBody>
    <PageHeader title="Profile" description="Review your account and security details." />

    <FormPanel as="form" size="md" class="grid gap-5 space-y-0" @submit.prevent="saveProfile">
      <FormSection title="Account">
        <FormField v-slot="{ id }" label="Email">
          <Input :id="id" :model-value="auth.user?.email" disabled />
        </FormField>
        <FormField v-slot="{ id }" label="Display name">
          <Input :id="id" v-model="displayName" autocomplete="name" />
        </FormField>
        <div>
          <StatusMessage>Roles</StatusMessage>
          <p class="mt-1 text-sm">{{ auth.roles.join(', ') || 'No roles assigned' }}</p>
        </div>
      </FormSection>
      <StatusMessage v-if="auth.error" tone="error">{{ auth.error }}</StatusMessage>
      <StatusMessage v-else-if="saved" tone="muted" role="status">Profile saved.</StatusMessage>
      <div class="flex flex-wrap gap-2">
        <Button type="submit" :disabled="auth.loading">
          {{ auth.loading ? 'Saving…' : 'Save profile' }}
        </Button>
        <Button as-child variant="outline">
          <router-link to="/change-password">Change password</router-link>
        </Button>
      </div>
    </FormPanel>
  </PageBody>
</template>
