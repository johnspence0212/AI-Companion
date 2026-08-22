<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { Button, FormPanel, Input, Label, StatusMessage } from '@/ui'
import { appName } from '@/config'

const currentPassword = ref('')
const newPassword = ref('')
const confirmPassword = ref('')
const validationError = ref<string | null>(null)
const auth = useAuthStore()
const router = useRouter()

async function onSubmit() {
  validationError.value = null
  if (newPassword.value !== confirmPassword.value) {
    validationError.value = 'New passwords do not match.'
    return
  }
  await auth.changePassword(currentPassword.value, newPassword.value)
  await router.replace('/')
}
</script>

<template>
  <FormPanel as="form" size="sm" @submit.prevent="onSubmit">
    <div class="space-y-1 text-center">
      <h1 class="text-2xl font-semibold">Change your password</h1>
      <StatusMessage>
        <template v-if="auth.mustChangePassword">
          A new password is required before you can use {{ appName }}.
        </template>
        <template v-else>Update the password for your account.</template>
      </StatusMessage>
    </div>
    <div class="space-y-2">
      <Label for="current-password">Current password</Label>
      <Input
        id="current-password"
        v-model="currentPassword"
        type="password"
        autocomplete="current-password"
        required
      />
    </div>
    <div class="space-y-2">
      <Label for="new-password">New password</Label>
      <Input
        id="new-password"
        v-model="newPassword"
        type="password"
        autocomplete="new-password"
        minlength="12"
        required
      />
    </div>
    <div class="space-y-2">
      <Label for="confirm-password">Confirm new password</Label>
      <Input
        id="confirm-password"
        v-model="confirmPassword"
        type="password"
        autocomplete="new-password"
        minlength="12"
        required
      />
    </div>
    <StatusMessage v-if="validationError || auth.error" tone="error">
      {{ validationError || auth.error }}
    </StatusMessage>
    <Button type="submit" class="w-full" :disabled="auth.loading">
      {{ auth.loading ? 'Changing password…' : 'Change password' }}
    </Button>
  </FormPanel>
</template>
