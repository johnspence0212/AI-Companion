import type { Meta, StoryObj } from '@storybook/vue3-vite'
import { ref } from 'vue'
import { Button, FormField, FormSection, FormSlideout, Input } from '@/ui'

const meta = {
  title: 'UI/Chrome/FormSlideout',
  component: FormSlideout,
  tags: ['autodocs'],
} satisfies Meta<typeof FormSlideout>

export default meta
type Story = StoryObj<typeof meta>

export const Default: Story = {
  args: {
    open: true,
    title: 'Add user',
  },
  render: () => ({
    components: { Button, FormField, FormSection, FormSlideout, Input },
    setup() {
      const open = ref(true)
      return { open }
    },
    template: `
      <div>
        <Button shape="square" @click="open = true">Add user</Button>
        <FormSlideout
          v-model:open="open"
          title="Add user"
          description="Creates an account with a temporary password."
          submit-label="Create"
        >
          <FormSection title="Account">
            <FormField label="Email" required v-slot="{ id }">
              <Input :id="id" type="email" />
            </FormField>
          </FormSection>
        </FormSlideout>
      </div>
    `,
  }),
}
