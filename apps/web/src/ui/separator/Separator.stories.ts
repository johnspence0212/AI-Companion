import type { Meta, StoryObj } from '@storybook/vue3-vite'
import { Separator } from '@/ui'

const meta = {
  title: 'UI/Primitives/Separator',
  component: Separator,
  tags: ['autodocs'],
} satisfies Meta<typeof Separator>

export default meta
type Story = StoryObj<typeof meta>

export const Horizontal: Story = {
  render: () => ({
    components: { Separator },
    template: `
      <div class="max-w-sm space-y-3">
        <p class="text-sm">Above</p>
        <Separator />
        <p class="text-sm">Below</p>
      </div>
    `,
  }),
}

export const Vertical: Story = {
  render: () => ({
    components: { Separator },
    template: `
      <div class="flex h-8 items-center gap-3">
        <span class="text-sm">Left</span>
        <Separator orientation="vertical" />
        <span class="text-sm">Right</span>
      </div>
    `,
  }),
}
