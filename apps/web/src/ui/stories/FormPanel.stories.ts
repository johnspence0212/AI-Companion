import type { Meta, StoryObj } from '@storybook/vue3-vite'
import { Button, FormField, FormPanel, Input } from '@/ui'

const meta = {
  title: 'UI/Chrome/FormPanel',
  component: FormPanel,
  tags: ['autodocs'],
} satisfies Meta<typeof FormPanel>

export default meta
type Story = StoryObj<typeof meta>

export const Default: Story = {
  render: () => ({
    components: { Button, FormField, FormPanel, Input },
    template: `
      <FormPanel as="form" @submit.prevent>
        <h2 class="text-lg font-semibold">Sign in</h2>
        <FormField label="Email" required v-slot="{ id }">
          <Input :id="id" type="email" autocomplete="username" />
        </FormField>
        <Button type="submit" class="w-full" shape="square">Sign in</Button>
      </FormPanel>
    `,
  }),
}
