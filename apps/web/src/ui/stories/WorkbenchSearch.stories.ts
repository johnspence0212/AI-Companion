import type { Meta, StoryObj } from '@storybook/vue3-vite'
import { WorkbenchSearch } from '@/ui'

const meta = {
  title: 'UI/Chrome/WorkbenchSearch',
  component: WorkbenchSearch,
  tags: ['autodocs'],
} satisfies Meta<typeof WorkbenchSearch>

export default meta
type Story = StoryObj<typeof meta>

export const TopBar: Story = {
  render: () => ({
    components: { WorkbenchSearch },
    template: `
      <div class="flex h-16 items-center border-b px-4">
        <WorkbenchSearch />
      </div>
    `,
  }),
}
