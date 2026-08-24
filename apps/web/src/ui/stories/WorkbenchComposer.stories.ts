import type { Meta, StoryObj } from '@storybook/vue3-vite'
import { ref } from 'vue'
import { WorkbenchComposer } from '@/ui'

const meta = {
  title: 'UI/Chrome/WorkbenchComposer',
  component: WorkbenchComposer,
  tags: ['autodocs'],
} satisfies Meta<typeof WorkbenchComposer>

export default meta
type Story = StoryObj<typeof meta>

export const Capture: Story = {
  args: {
    modelValue: '',
    placeholder: 'Capture a thought',
    submitLabel: 'Capture',
  },
  render: (args) => ({
    components: { WorkbenchComposer },
    setup() {
      const draft = ref(args.modelValue)
      return { args, draft }
    },
    template: `
      <WorkbenchComposer
        v-model="draft"
        :placeholder="args.placeholder"
        :submit-label="args.submitLabel"
      />
    `,
  }),
}
