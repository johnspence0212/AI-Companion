<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { usersApi } from '@/api/usersApi'
import { rolesApi, type Role } from '@/api/rolesApi'
import type { ManagedUser } from '@/api/types/schema'
import { useAuthStore } from '@/stores/auth'
import {
  Button,
  DataList,
  DataListEmpty,
  DataListItem,
  FormField,
  FormSection,
  FormSlideout,
  Input,
  MultiSelect,
  PageBody,
  PageHeader,
  StatusMessage,
} from '@/ui'

const auth = useAuthStore()
const users = ref<ManagedUser[]>([])
const roles = ref<Role[]>([])
const loading = ref(false)
const saving = ref(false)
const error = ref<string | null>(null)
const slideoutOpen = ref(false)
const editingUser = ref<ManagedUser | null>(null)
const email = ref('')
const displayName = ref('')
const temporaryPassword = ref('')
const selectedRoles = ref<string[]>([])
const isDisabled = ref(false)

const roleOptions = computed(() =>
  roles.value.map((role) => ({ value: role.name, label: role.name })),
)
const title = computed(() => (editingUser.value ? 'Edit user' : 'Add user'))
const description = computed(() =>
  editingUser.value
    ? 'Update global account access and status.'
    : 'Create a global account with a temporary password.',
)

function resetForm() {
  editingUser.value = null
  email.value = ''
  displayName.value = ''
  temporaryPassword.value = ''
  selectedRoles.value = []
  isDisabled.value = false
}

function openCreate() {
  resetForm()
  slideoutOpen.value = true
}

function openEdit(user: ManagedUser) {
  editingUser.value = user
  email.value = user.email
  displayName.value = user.displayName ?? ''
  selectedRoles.value = [...user.roles]
  isDisabled.value = user.isDisabled
  slideoutOpen.value = true
}

function setOpen(open: boolean) {
  slideoutOpen.value = open
  if (!open) resetForm()
}

async function load() {
  loading.value = true
  error.value = null
  try {
    const [userItems, roleItems] = await Promise.all([
      usersApi.list(),
      auth.hasPermission('users.manage') ? rolesApi.list() : Promise.resolve([]),
    ])
    users.value = userItems
    roles.value = roleItems
  } catch (err) {
    error.value = err instanceof Error ? err.message : 'Failed to load users'
  } finally {
    loading.value = false
  }
}

async function save() {
  saving.value = true
  error.value = null
  try {
    if (editingUser.value) {
      await usersApi.update(editingUser.value.id, {
        roles: selectedRoles.value,
        isDisabled: isDisabled.value,
      })
    } else {
      await usersApi.create({
        email: email.value.trim(),
        displayName: displayName.value.trim() || undefined,
        temporaryPassword: temporaryPassword.value,
        roles: selectedRoles.value,
      })
    }
    setOpen(false)
    await load()
  } catch (err) {
    error.value = err instanceof Error ? err.message : 'Failed to save user'
  } finally {
    saving.value = false
  }
}

onMounted(load)
</script>

<template>
  <PageBody>
    <PageHeader eyebrow="Settings" title="Users" description="Manage global application accounts.">
      <template #actions>
        <Button v-if="auth.hasPermission('users.manage')" shape="square" @click="openCreate">
          Add user
        </Button>
      </template>
    </PageHeader>

    <StatusMessage v-if="error" tone="error">{{ error }}</StatusMessage>
    <StatusMessage v-if="loading">Loading…</StatusMessage>

    <DataList v-else>
      <DataListItem
        v-for="user in users"
        :key="user.id"
        :title="user.email"
        :description="
          [
            user.displayName || 'No display name',
            user.roles.join(', ') || 'No roles',
            user.isDisabled ? 'Disabled' : null,
            user.mustChangePassword ? 'Password change required' : null,
          ]
            .filter(Boolean)
            .join(' · ')
        "
      >
        <template #actions>
          <Button
            v-if="auth.hasPermission('users.manage')"
            variant="outline"
            size="sm"
            @click="openEdit(user)"
          >
            Edit
          </Button>
        </template>
      </DataListItem>
      <DataListEmpty v-if="users.length === 0">No users found.</DataListEmpty>
    </DataList>

    <FormSlideout
      :open="slideoutOpen"
      :title="title"
      :description="description"
      :submit-label="editingUser ? 'Save changes' : 'Add user'"
      :pending="saving"
      @update:open="setOpen"
      @submit="save"
    >
      <FormSection title="Account">
        <FormField v-slot="{ id }" label="Email" required>
          <Input
            :id="id"
            v-model="email"
            type="email"
            autocomplete="email"
            required
            :disabled="editingUser !== null"
          />
        </FormField>
        <FormField v-if="!editingUser" v-slot="{ id }" label="Display name">
          <Input :id="id" v-model="displayName" autocomplete="name" />
        </FormField>
        <FormField
          v-if="!editingUser"
          v-slot="{ id }"
          label="Temporary password"
          description="The user will be required to change this password."
          required
        >
          <Input
            :id="id"
            v-model="temporaryPassword"
            type="password"
            autocomplete="new-password"
            minlength="12"
            required
          />
        </FormField>
      </FormSection>
      <FormSection title="Access">
        <FormField v-slot="{ id, ariaLabelledby }" label="Roles">
          <MultiSelect
            :id="id"
            v-model="selectedRoles"
            :labelled-by="ariaLabelledby"
            :options="roleOptions"
            placeholder="Select roles"
          />
        </FormField>
        <label v-if="editingUser" class="flex items-center gap-2 text-sm">
          <input v-model="isDisabled" type="checkbox" class="size-4 rounded border border-input" />
          Disable this account
        </label>
      </FormSection>
    </FormSlideout>
  </PageBody>
</template>
