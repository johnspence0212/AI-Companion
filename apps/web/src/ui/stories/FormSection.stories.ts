import type { Meta, StoryObj } from '@storybook/vue3-vite'
import { FormField, FormSection, Input } from '@/ui'

const meta = {
  title: 'UI/Chrome/FormSection',
  component: FormSection,
  tags: ['autodocs'],
} satisfies Meta<typeof FormSection>

export default meta
type Story = StoryObj<typeof meta>

export const Default: Story = {
  args: {
    title: 'Account',
  },
  render: () => ({
    components: { FormField, FormSection, Input },
    template: `
      <FormSection title="Account" class="max-w-md">
        <FormField label="Email" required v-slot="{ id }">
          <Input :id="id" type="email" />
        </FormField>
        <FormField label="Display name" v-slot="{ id }">
          <Input :id="id" />
        </FormField>
      </FormSection>
    `,
  }),
}
