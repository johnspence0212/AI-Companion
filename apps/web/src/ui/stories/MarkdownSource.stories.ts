import type { Meta, StoryObj } from '@storybook/vue3-vite'
import { ref } from 'vue'
import { MarkdownSource } from '@/ui'

const meta = {
  title: 'UI/Chrome/MarkdownSource',
  component: MarkdownSource,
  tags: ['autodocs'],
} satisfies Meta<typeof MarkdownSource>

export default meta
type Story = StoryObj<typeof meta>

export const SourceAndPreview: Story = {
  args: {
    modelValue: '',
  },
  render: () => ({
    components: { MarkdownSource },
    setup() {
      const source = ref(`# Project Context

A fenced example:

\`\`\`ts
const token = "inside-the-fence"
\`\`\`
`)
      return { source }
    },
    template: `
      <MarkdownSource v-model="source" label="Markdown" />
    `,
  }),
}
