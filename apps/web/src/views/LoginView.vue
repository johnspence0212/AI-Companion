<script setup lang="ts">
import { ref } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { Button, FormPanel, Input, Label, StatusMessage } from '@/ui'
import { appName } from '@/config'

const email = ref('')
const password = ref('')
const auth = useAuthStore()
const router = useRouter()
const route = useRoute()

async function onSubmit() {
  await auth.login(email.value.trim(), password.value)
  const redirect = auth.mustChangePassword
    ? '/change-password'
    : (route.query.redirect as string) || '/'
  await router.push(redirect)
}
</script>

<template>
  <FormPanel as="form" size="sm" @submit.prevent="onSubmit">
    <div class="space-y-1 text-center">
      <h1 class="text-2xl font-semibold">Sign in</h1>
      <StatusMessage>{{ appName }}</StatusMessage>
    </div>
    <div class="space-y-2">
      <Label for="email">Email</Label>
      <Input id="email" v-model="email" type="email" autocomplete="email" required />
    </div>
    <div class="space-y-2">
      <Label for="password">Password</Label>
      <Input
        id="password"
        v-model="password"
        type="password"
        autocomplete="current-password"
        required
      />
    </div>
    <StatusMessage v-if="auth.error" tone="error">{{ auth.error }}</StatusMessage>
    <Button type="submit" class="w-full" :disabled="auth.loading">
      {{ auth.loading ? 'Signing in…' : 'Sign in' }}
    </Button>
  </FormPanel>
</template>
