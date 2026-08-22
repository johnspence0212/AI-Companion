import type { Meta, StoryObj } from '@storybook/vue3-vite'
import { ref } from 'vue'
import { FormField, MultiSelect } from '@/ui'
import type { MultiSelectOption } from '@/ui'

const options: MultiSelectOption[] = [
  { value: 'users.read', label: 'users.read' },
  { value: 'users.manage', label: 'users.manage' },
  { value: 'roles.read', label: 'roles.read' },
  { value: 'audit.read', label: 'audit.read' },
]

const meta = {
  title: 'UI/Chrome/MultiSelect',
  component: MultiSelect,
  tags: ['autodocs'],
} satisfies Meta<typeof MultiSelect>

export default meta
type Story = StoryObj<typeof meta>

export const Default: Story = {
  args: {
    modelValue: ['users.read'],
    options,
  },
  render: () => ({
    components: { FormField, MultiSelect },
    setup() {
      const selected = ref<string[]>(['users.read'])
      return { selected, options }
    },
    template: `
      <FormField class="max-w-sm" label="Permissions" v-slot="{ id, 'aria-labelledby': labelledBy }">
        <MultiSelect
          :id="id"
          v-model="selected"
          :options="options"
          :labelled-by="labelledBy"
          placeholder="Select permissions"
        />
      </FormField>
    `,
  }),
}
