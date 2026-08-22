import type { Meta, StoryObj } from '@storybook/vue3-vite'
import { Skeleton } from '@/ui'

const meta = {
  title: 'UI/Primitives/Skeleton',
  component: Skeleton,
  tags: ['autodocs'],
} satisfies Meta<typeof Skeleton>

export default meta
type Story = StoryObj<typeof meta>

export const Default: Story = {
  render: () => ({
    components: { Skeleton },
    template: `
      <div class="max-w-sm space-y-3">
        <Skeleton class="h-4 w-1/3" />
        <Skeleton class="h-4 w-full" />
        <Skeleton class="h-4 w-2/3" />
      </div>
    `,
  }),
}
