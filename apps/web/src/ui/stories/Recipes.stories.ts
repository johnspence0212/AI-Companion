import type { Meta, StoryObj } from '@storybook/vue3-vite'
import { ref } from 'vue'
import {
  Button,
  DataList,
  DataListItem,
  DataTable,
  FormField,
  FormPanel,
  FormSection,
  FormSlideout,
  GuestShell,
  Input,
  PageBody,
  PageHeader,
  StatusMessage,
} from '@/ui'

const meta = {
  title: 'UI/Recipes',
  tags: ['autodocs'],
} satisfies Meta

export default meta
type Story = StoryObj<typeof meta>

export const ListCreateEdit: Story = {
  name: 'List + create/edit',
  render: () => ({
    components: {
      Button,
      DataList,
      DataListItem,
      FormField,
      FormSection,
      FormSlideout,
      Input,
      PageBody,
      PageHeader,
    },
    setup() {
      const open = ref(false)
      return { open }
    },
    template: `
      <PageBody>
        <PageHeader eyebrow="Settings" title="Users" description="Admin-created accounts.">
          <template #actions>
            <Button shape="square" @click="open = true">Add user</Button>
          </template>
        </PageHeader>
        <DataList>
          <DataListItem title="Ada Admin" description="admin@enterprisestarter.local">
            <template #actions>
              <Button variant="outline" size="sm" shape="square" @click="open = true">Edit</Button>
            </template>
          </DataListItem>
        </DataList>
        <FormSlideout v-model:open="open" title="Add user" submit-label="Create">
          <FormSection title="Account">
            <FormField label="Email" required v-slot="{ id }">
              <Input :id="id" type="email" />
            </FormField>
          </FormSection>
        </FormSlideout>
      </PageBody>
    `,
  }),
}

export const GuestAuthForm: Story = {
  name: 'Guest auth form',
  parameters: {
    layout: 'fullscreen',
  },
  render: () => ({
    components: { Button, FormField, FormPanel, GuestShell, Input, StatusMessage },
    template: `
      <GuestShell>
        <FormPanel as="form" @submit.prevent>
          <h1 class="text-lg font-semibold">Sign in</h1>
          <FormField label="Email" required v-slot="{ id }">
            <Input :id="id" type="email" autocomplete="username" />
          </FormField>
          <FormField label="Password" required v-slot="{ id }">
            <Input :id="id" type="password" autocomplete="current-password" />
          </FormField>
          <StatusMessage tone="error">Invalid email or password</StatusMessage>
          <Button type="submit" class="w-full" shape="square">Sign in</Button>
        </FormPanel>
      </GuestShell>
    `,
  }),
}

export const DenseTable: Story = {
  name: 'Dense table',
  render: () => ({
    components: { DataTable, PageBody, PageHeader },
    template: `
      <PageBody>
        <PageHeader eyebrow="Settings" title="Security audit" description="Login and administrative events." />
        <DataTable>
          <thead>
            <tr class="border-b text-muted-foreground">
              <th class="px-4 py-3 font-medium">When</th>
              <th class="px-4 py-3 font-medium">Event</th>
              <th class="px-4 py-3 font-medium">Outcome</th>
            </tr>
          </thead>
          <tbody>
            <tr class="border-b">
              <td class="px-4 py-3">2026-08-12 21:55</td>
              <td class="px-4 py-3">login.succeeded</td>
              <td class="px-4 py-3">Succeeded</td>
            </tr>
            <tr>
              <td class="px-4 py-3">2026-08-12 21:56</td>
              <td class="px-4 py-3">role.updated</td>
              <td class="px-4 py-3">Succeeded</td>
            </tr>
          </tbody>
        </DataTable>
      </PageBody>
    `,
  }),
}
