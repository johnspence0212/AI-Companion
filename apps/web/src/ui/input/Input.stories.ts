import type { Meta, StoryObj } from '@storybook/vue3-vite'
import { Input, Label } from '@/ui'

const meta = {
  title: 'UI/Primitives/Input',
  component: Input,
  tags: ['autodocs'],
} satisfies Meta<typeof Input>

export default meta
type Story = StoryObj<typeof meta>

export const Default: Story = {
  render: () => ({
    components: { Input },
    template: '<Input class="max-w-sm" placeholder="Email" type="email" />',
  }),
}

export const WithLabel: Story = {
  render: () => ({
    components: { Input, Label },
    template: `
      <div class="grid max-w-sm gap-2">
        <Label for="catalogue-email">Email</Label>
        <Input id="catalogue-email" type="email" placeholder="admin@enterprisestarter.local" />
      </div>
    `,
  }),
}

export const Disabled: Story = {
  render: () => ({
    components: { Input },
    template: '<Input class="max-w-sm" disabled placeholder="Unavailable" />',
  }),
}
