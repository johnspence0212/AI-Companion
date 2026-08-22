import type { Meta, StoryObj } from '@storybook/vue3-vite'
import { Label } from '@/ui'

const meta = {
  title: 'UI/Primitives/Label',
  component: Label,
  tags: ['autodocs'],
} satisfies Meta<typeof Label>

export default meta
type Story = StoryObj<typeof meta>

export const Default: Story = {
  render: () => ({
    components: { Label },
    template: '<Label>Display name</Label>',
  }),
}
