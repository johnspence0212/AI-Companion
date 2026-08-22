<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { rolesApi, type PermissionDefinition, type Role } from '@/api/rolesApi'
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
  PageBody,
  PageHeader,
  StatusMessage,
} from '@/ui'

const auth = useAuthStore()
const catalog = ref<PermissionDefinition[]>([])
const roles = ref<Role[]>([])
const loading = ref(false)
const saving = ref(false)
const error = ref<string | null>(null)
const slideoutOpen = ref(false)
const editingRole = ref<Role | null>(null)
const roleName = ref('')
const selectedPermissions = ref<string[]>([])

const groupedCatalog = computed(() => {
  const groups = new Map<string, PermissionDefinition[]>()
  for (const permission of catalog.value) {
    groups.set(permission.group, [...(groups.get(permission.group) ?? []), permission])
  }
  return [...groups.entries()]
})

function openCreate() {
  editingRole.value = null
  roleName.value = ''
  selectedPermissions.value = []
  slideoutOpen.value = true
}

function openEdit(role: Role) {
  editingRole.value = role
  roleName.value = role.name
  selectedPermissions.value = [...role.permissions]
  slideoutOpen.value = true
}

function setOpen(open: boolean) {
  slideoutOpen.value = open
  if (!open) {
    editingRole.value = null
    roleName.value = ''
    selectedPermissions.value = []
  }
}

function togglePermission(key: string, checked: boolean) {
  selectedPermissions.value = checked
    ? [...new Set([...selectedPermissions.value, key])]
    : selectedPermissions.value.filter((permission) => permission !== key)
}

async function load() {
  loading.value = true
  error.value = null
  try {
    const [permissionItems, roleItems] = await Promise.all([rolesApi.catalog(), rolesApi.list()])
    catalog.value = permissionItems
    roles.value = roleItems
  } catch (err) {
    error.value = err instanceof Error ? err.message : 'Failed to load roles'
  } finally {
    loading.value = false
  }
}

async function save() {
  saving.value = true
  error.value = null
  const payload = { name: roleName.value.trim(), permissions: selectedPermissions.value }
  try {
    if (editingRole.value) {
      await rolesApi.update(editingRole.value.id, payload)
    } else {
      await rolesApi.create(payload)
    }
    setOpen(false)
    await load()
  } catch (err) {
    error.value = err instanceof Error ? err.message : 'Failed to save role'
  } finally {
    saving.value = false
  }
}

async function remove(role: Role) {
  error.value = null
  try {
    await rolesApi.remove(role.id)
    await load()
  } catch (err) {
    error.value = err instanceof Error ? err.message : 'Failed to delete role'
  }
}

onMounted(load)
</script>

<template>
  <PageBody>
    <PageHeader
      eyebrow="Settings"
      title="Roles"
      description="Bundle permissions into global application roles."
    >
      <template #actions>
        <Button v-if="auth.hasPermission('roles.manage')" shape="square" @click="openCreate">
          Add role
        </Button>
      </template>
    </PageHeader>

    <StatusMessage v-if="error" tone="error">{{ error }}</StatusMessage>
    <StatusMessage v-if="loading">Loading…</StatusMessage>

    <DataList v-else>
      <DataListItem
        v-for="role in roles"
        :key="role.id"
        :description="role.permissions.join(', ') || 'No permissions'"
      >
        <template #title>
          {{ role.name }}
          <span v-if="role.isProtected" class="text-xs text-muted-foreground">(protected)</span>
        </template>
        <template #actions>
          <template v-if="auth.hasPermission('roles.manage')">
            <Button variant="outline" size="sm" @click="openEdit(role)">Edit</Button>
            <Button v-if="!role.isProtected" variant="outline" size="sm" @click="remove(role)">
              Delete
            </Button>
          </template>
        </template>
      </DataListItem>
      <DataListEmpty v-if="roles.length === 0">No roles found.</DataListEmpty>
    </DataList>

    <FormSlideout
      :open="slideoutOpen"
      :title="editingRole ? 'Edit role' : 'Add role'"
      description="Choose a name and the permissions granted by this role."
      :submit-label="editingRole ? 'Save changes' : 'Add role'"
      :pending="saving"
      allow-fullscreen
      @update:open="setOpen"
      @submit="save"
    >
      <FormSection title="Role">
        <FormField v-slot="{ id }" label="Name" required>
          <Input
            :id="id"
            v-model="roleName"
            required
            :disabled="editingRole?.isProtected === true"
          />
        </FormField>
      </FormSection>
      <FormSection title="Permissions">
        <fieldset v-for="[group, permissions] in groupedCatalog" :key="group" class="space-y-2">
          <legend class="text-xs font-medium tracking-wide text-muted-foreground uppercase">
            {{ group }}
          </legend>
          <label
            v-for="permission in permissions"
            :key="permission.key"
            class="flex items-start gap-2 text-sm"
          >
            <input
              type="checkbox"
              class="mt-0.5 size-4 rounded border border-input"
              :checked="selectedPermissions.includes(permission.key)"
              @change="
                togglePermission(permission.key, ($event.target as HTMLInputElement).checked)
              "
            />
            <span>{{ permission.label }}</span>
          </label>
        </fieldset>
      </FormSection>
    </FormSlideout>
  </PageBody>
</template>
