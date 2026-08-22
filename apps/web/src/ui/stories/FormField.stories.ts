import type { Meta, StoryObj } from '@storybook/vue3-vite'
import { FormField, Input } from '@/ui'

const meta = {
  title: 'UI/Chrome/FormField',
  component: FormField,
  tags: ['autodocs'],
} satisfies Meta<typeof FormField>

export default meta
type Story = StoryObj<typeof meta>

export const Default: Story = {
  args: {
    label: 'Email',
    required: true,
  },
  render: () => ({
    components: { FormField, Input },
    template: `
      <FormField class="max-w-sm" label="Email" required v-slot="{ id }">
        <Input :id="id" type="email" />
      </FormField>
    `,
  }),
}
