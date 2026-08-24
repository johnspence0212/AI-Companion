import type { Meta, StoryObj } from '@storybook/vue3-vite'
import { Button, PageHeader } from '@/ui'

const meta = {
  title: 'UI/Chrome/PageHeader',
  component: PageHeader,
  tags: ['autodocs'],
  args: {
    title: 'Users',
    description: 'Admin-created accounts with temporary passwords.',
    eyebrow: 'Settings',
  },
} satisfies Meta<typeof PageHeader>

export default meta
type Story = StoryObj<typeof meta>

export const Default: Story = {
  render: (args) => ({
    components: { PageHeader },
    setup: () => ({ args }),
    template: '<PageHeader v-bind="args" />',
  }),
}

export const WithActions: Story = {
  render: (args) => ({
    components: { Button, PageHeader },
    setup: () => ({ args }),
    template: `
      <PageHeader v-bind="args">
        <template #actions>
          <Button shape="square">Add user</Button>
        </template>
      </PageHeader>
    `,
  }),
}

export const Compact: Story = {
  args: {
    title: 'Today',
    description: 'Monday, August 24',
    eyebrow: undefined,
    size: 'compact',
  },
  render: (args) => ({
    components: { Button, PageHeader },
    setup: () => ({ args }),
    template: `
      <PageHeader v-bind="args">
        <template #actions>
          <Button size="sm" shape="square">New</Button>
        </template>
      </PageHeader>
    `,
  }),
}
