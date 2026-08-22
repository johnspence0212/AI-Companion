import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import { authApi } from '@/api/authApi'
import { ApiError } from '@/api/base/client'
import type { User } from '@/api/types/schema'

function messageFromError(error: unknown, fallback: string): string {
  if (error instanceof ApiError) {
    if (error.status === 400) {
      return error.message || fallback
    }
    if (error.status === 423) {
      return 'Account is locked. Try again later.'
    }
    if (error.status === 429) {
      return 'Too many attempts. Wait a moment and try again.'
    }
    if (error.message) {
      return error.message
    }
  }

  if (error instanceof Error) {
    const msg = error.message
    if (msg.includes('Failed to fetch') || msg.includes('NetworkError')) {
      return 'Cannot reach the API. Check the Aspire dashboard for the correct web URL (use the proxied app).'
    }
    if (msg.length > 0 && msg.length < 200) {
      return msg
    }
  }

  return fallback
}

export const useAuthStore = defineStore('auth', () => {
  const user = ref<User | null>(null)
  const loading = ref(false)
  const error = ref<string | null>(null)
  const hydrated = ref(false)

  const isAuthenticated = computed(() => !!user.value)
  const permissions = computed(() => user.value?.permissions ?? [])
  const roles = computed(() => user.value?.roles ?? [])
  const mustChangePassword = computed(() => user.value?.mustChangePassword === true)
  const hasPermission = (permission: string) => permissions.value.includes(permission)

  async function login(email: string, password: string) {
    loading.value = true
    error.value = null
    try {
      user.value = await authApi.login({ email, password })
    } catch (err) {
      error.value = messageFromError(err, 'Invalid email or password')
      throw err
    } finally {
      loading.value = false
    }
  }

  async function changePassword(currentPassword: string, newPassword: string) {
    loading.value = true
    error.value = null
    try {
      user.value = await authApi.changePassword({
        currentPassword,
        newPassword,
      })
    } catch (err) {
      error.value = messageFromError(err, 'Password change failed')
      throw err
    } finally {
      loading.value = false
    }
  }

  async function updateProfile(displayName: string) {
    loading.value = true
    error.value = null
    try {
      user.value = await authApi.updateProfile({ displayName })
    } catch (err) {
      error.value = messageFromError(err, 'Profile update failed')
      throw err
    } finally {
      loading.value = false
    }
  }

  async function fetchUser() {
    user.value = await authApi.me()
  }

  async function hydrate() {
    try {
      await fetchUser()
    } catch {
      user.value = null
    } finally {
      hydrated.value = true
    }
  }

  async function logout() {
    try {
      await authApi.logout()
    } catch {
      // Local session clear still required if the API is unreachable.
    } finally {
      user.value = null
      error.value = null
    }
  }

  return {
    user,
    loading,
    error,
    hydrated,
    isAuthenticated,
    permissions,
    roles,
    mustChangePassword,
    hasPermission,
    login,
    updateProfile,
    changePassword,
    fetchUser,
    hydrate,
    logout,
  }
})
