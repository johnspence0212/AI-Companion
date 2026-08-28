import type { Meta, StoryObj } from '@storybook/vue3-vite'
import { WorkbenchInbox } from '@/ui'

const meta = {
  title: 'UI/Chrome/WorkbenchInbox',
  component: WorkbenchInbox,
  tags: ['autodocs'],
} satisfies Meta<typeof WorkbenchInbox>

export default meta
type Story = StoryObj<typeof meta>

export const TopBar: Story = {
  render: () => ({
    components: { WorkbenchInbox },
    template: `
      <div class="flex h-16 items-center justify-center border-b px-4">
        <WorkbenchInbox />
      </div>
    `,
  }),
}
