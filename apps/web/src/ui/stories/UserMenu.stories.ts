import type { Meta, StoryObj } from '@storybook/vue3-vite'
import { UserMenu } from '@/ui'

const meta = {
  title: 'UI/Chrome/UserMenu',
  component: UserMenu,
  tags: ['autodocs'],
} satisfies Meta<typeof UserMenu>

export default meta
type Story = StoryObj<typeof meta>

export const Default: Story = {
  render: () => ({
    components: { UserMenu },
    template: `
      <div class="flex justify-end p-6">
        <UserMenu />
      </div>
    `,
  }),
}
