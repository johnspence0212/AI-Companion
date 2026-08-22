import type { Meta, StoryObj } from '@storybook/vue3-vite'
import { Button, FormField, FormPanel, GuestShell, Input, StatusMessage } from '@/ui'

const meta = {
  title: 'UI/Chrome/GuestShell',
  component: GuestShell,
  tags: ['autodocs'],
  parameters: {
    layout: 'fullscreen',
  },
} satisfies Meta<typeof GuestShell>

export default meta
type Story = StoryObj<typeof meta>

export const Default: Story = {
  render: () => ({
    components: { Button, FormField, FormPanel, GuestShell, Input, StatusMessage },
    template: `
      <GuestShell>
        <FormPanel as="form" @submit.prevent>
          <h1 class="text-lg font-semibold">Sign in</h1>
          <FormField label="Email" required v-slot="{ id }">
            <Input :id="id" type="email" />
          </FormField>
          <StatusMessage tone="muted">Use the seeded admin on first boot.</StatusMessage>
          <Button type="submit" class="w-full" shape="square">Sign in</Button>
        </FormPanel>
      </GuestShell>
    `,
  }),
}
