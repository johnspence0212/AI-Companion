import type { Meta, StoryObj } from '@storybook/vue3-vite'
import { StatusMessage } from '@/ui'

const meta = {
  title: 'UI/Chrome/StatusMessage',
  component: StatusMessage,
  tags: ['autodocs'],
  argTypes: {
    tone: { control: 'select', options: ['muted', 'error', 'success'] },
  },
} satisfies Meta<typeof StatusMessage>

export default meta
type Story = StoryObj<typeof meta>

export const Muted: Story = {
  args: { tone: 'muted' },
  render: (args) => ({
    components: { StatusMessage },
    setup: () => ({ args }),
    template: '<StatusMessage v-bind="args">No users match this filter.</StatusMessage>',
  }),
}

export const Error: Story = {
  args: { tone: 'error' },
  render: (args) => ({
    components: { StatusMessage },
    setup: () => ({ args }),
    template: '<StatusMessage v-bind="args">Failed to load users.</StatusMessage>',
  }),
}

export const Success: Story = {
  args: { tone: 'success' },
  render: (args) => ({
    components: { StatusMessage },
    setup: () => ({ args }),
    template: '<StatusMessage v-bind="args">Password updated.</StatusMessage>',
  }),
}
