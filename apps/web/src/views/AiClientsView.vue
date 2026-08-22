<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { aiClientsApi, type AiClient } from '@/api/aiClientsApi'
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

const clients = ref<AiClient[]>([])
const loading = ref(false)
const saving = ref(false)
const error = ref<string | null>(null)
const notice = ref<string | null>(null)
const slideoutOpen = ref(false)
const name = ref('')
const oneTimeSecret = ref<string | null>(null)
const copied = ref(false)

const activeClients = computed(() => clients.value.filter((client) => !client.archivedAt))
const revokedClients = computed(() => clients.value.filter((client) => client.archivedAt))

function resetCreate() {
  name.value = ''
  oneTimeSecret.value = null
  copied.value = false
}

function openCreate() {
  resetCreate()
  slideoutOpen.value = true
}

function setOpen(open: boolean) {
  slideoutOpen.value = open
  if (!open) {
    resetCreate()
  }
}

async function load() {
  loading.value = true
  error.value = null
  try {
    clients.value = await aiClientsApi.list()
  } catch (e) {
    error.value = e instanceof Error ? e.message : 'Failed to load AI Clients'
    throw e
  } finally {
    loading.value = false
  }
}

async function createClient() {
  if (!name.value.trim()) {
    error.value = 'Name is required.'
    return
  }

  saving.value = true
  error.value = null
  notice.value = null
  copied.value = false
  try {
    const created = await aiClientsApi.create(name.value.trim())
    oneTimeSecret.value = created.secret
    await load()
    notice.value = 'Copy the secret now. It will not be shown again.'
  } catch (e) {
    error.value = e instanceof Error ? e.message : 'Failed to create AI Client'
    throw e
  } finally {
    saving.value = false
  }
}

async function copySecret() {
  if (!oneTimeSecret.value) {
    return
  }

  await navigator.clipboard.writeText(oneTimeSecret.value)
  copied.value = true
}

async function revoke(client: AiClient) {
  saving.value = true
  error.value = null
  try {
    await aiClientsApi.revoke(client.id)
    await load()
    notice.value = `${client.name} was revoked. That secret can no longer call /mcp.`
  } catch (e) {
    error.value = e instanceof Error ? e.message : 'Failed to revoke AI Client'
    throw e
  } finally {
    saving.value = false
  }
}

onMounted(() => {
  void load()
})
</script>

<template>
  <PageBody>
    <PageHeader
      title="AI Clients"
      description="Credentialed integrations that act on your data. The secret is shown once. There is no in-app AI."
    >
      <template #actions>
        <Button shape="square" @click="openCreate">New AI Client</Button>
      </template>
    </PageHeader>

    <StatusMessage v-if="error" tone="error">{{ error }}</StatusMessage>
    <StatusMessage v-else-if="notice" tone="success">{{ notice }}</StatusMessage>

    <DataList>
      <DataListEmpty v-if="!loading && activeClients.length === 0">
        No active AI Clients.
      </DataListEmpty>
      <DataListItem
        v-for="client in activeClients"
        :key="client.id"
        :title="client.name"
        :description="`Created ${client.createdAt}`"
      >
        <template #actions>
          <Button
            size="sm"
            variant="outline"
            shape="square"
            :disabled="saving"
            @click="revoke(client)"
          >
            Revoke
          </Button>
        </template>
      </DataListItem>
    </DataList>

    <DataList v-if="revokedClients.length" class="mt-6">
      <DataListItem
        v-for="client in revokedClients"
        :key="client.id"
        :title="client.name"
        description="Revoked"
      />
    </DataList>

    <FormSlideout
      :open="slideoutOpen"
      title="New AI Client"
      description="The bearer secret is shown once. Copy it before you close this panel."
      :submit-label="oneTimeSecret ? 'Done' : 'Create'"
      :pending="saving"
      @update:open="setOpen"
      @submit="oneTimeSecret ? setOpen(false) : createClient()"
    >
      <FormSection title="Client">
        <FormField label="Name" required>
          <Input
            v-model="name"
            name="ai-client-name"
            autocomplete="off"
            :disabled="!!oneTimeSecret"
          />
        </FormField>
        <template v-if="oneTimeSecret">
          <StatusMessage tone="success"
            >Copy this secret now. It is not stored in the browser.</StatusMessage
          >
          <FormField label="Secret">
            <Input
              :model-value="oneTimeSecret"
              name="ai-client-secret"
              readonly
              autocomplete="off"
            />
          </FormField>
          <Button type="button" shape="square" @click="copySecret">
            {{ copied ? 'Copied' : 'Copy secret' }}
          </Button>
        </template>
      </FormSection>
    </FormSlideout>
  </PageBody>
</template>
