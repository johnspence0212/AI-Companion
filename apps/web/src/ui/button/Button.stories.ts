import type { Meta, StoryObj } from '@storybook/vue3-vite'
import { Button } from '@/ui'

const meta = {
  title: 'UI/Primitives/Button',
  component: Button,
  tags: ['autodocs'],
  argTypes: {
    variant: {
      control: 'select',
      options: ['default', 'destructive', 'outline', 'secondary', 'ghost', 'link'],
    },
    size: {
      control: 'select',
      options: ['default', 'sm', 'lg', 'icon', 'icon-sm', 'icon-lg'],
    },
    shape: {
      control: 'select',
      options: ['circle', 'square'],
    },
  },
  args: {
    variant: 'default',
    size: 'default',
    shape: 'circle',
  },
} satisfies Meta<typeof Button>

export default meta
type Story = StoryObj<typeof meta>

export const Default: Story = {
  render: (args) => ({
    components: { Button },
    setup: () => ({ args }),
    template: '<Button v-bind="args">Save</Button>',
  }),
}

export const SquarePrimary: Story = {
  args: {
    shape: 'square',
  },
  render: (args) => ({
    components: { Button },
    setup: () => ({ args }),
    template: '<Button v-bind="args">Add user</Button>',
  }),
}

export const Outline: Story = {
  args: {
    variant: 'outline',
    shape: 'square',
  },
  render: (args) => ({
    components: { Button },
    setup: () => ({ args }),
    template: '<Button v-bind="args">Cancel</Button>',
  }),
}

export const Destructive: Story = {
  args: {
    variant: 'destructive',
    shape: 'square',
  },
  render: (args) => ({
    components: { Button },
    setup: () => ({ args }),
    template: '<Button v-bind="args">Disable</Button>',
  }),
}

export const Variants: Story = {
  render: () => ({
    components: { Button },
    template: `
      <div class="flex flex-wrap items-center gap-3">
        <Button>Default</Button>
        <Button variant="secondary">Secondary</Button>
        <Button variant="outline" shape="square">Outline</Button>
        <Button variant="ghost">Ghost</Button>
        <Button variant="link">Link</Button>
        <Button variant="destructive" shape="square">Destructive</Button>
      </div>
    `,
  }),
}
