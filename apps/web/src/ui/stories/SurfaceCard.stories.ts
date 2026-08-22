import type { Meta, StoryObj } from '@storybook/vue3-vite'
import { SurfaceCard } from '@/ui'

const meta = {
  title: 'UI/Chrome/SurfaceCard',
  component: SurfaceCard,
  tags: ['autodocs'],
} satisfies Meta<typeof SurfaceCard>

export default meta
type Story = StoryObj<typeof meta>

export const Static: Story = {
  render: () => ({
    components: { SurfaceCard },
    template: `
      <SurfaceCard class="max-w-sm">
        <p class="font-medium">Security audit</p>
        <p class="text-sm text-muted-foreground">Login and administrative events.</p>
      </SurfaceCard>
    `,
  }),
}

export const Linked: Story = {
  render: () => ({
    components: { SurfaceCard },
    template: `
      <SurfaceCard to="/admin/users" class="max-w-sm">
        <p class="font-medium">Users</p>
        <p class="text-sm text-muted-foreground">Open the users list.</p>
      </SurfaceCard>
    `,
  }),
}
