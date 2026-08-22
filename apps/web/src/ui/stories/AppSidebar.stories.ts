import type { Meta, StoryObj } from '@storybook/vue3-vite'
import { AppSidebar, SidebarProvider } from '@/ui'

const meta = {
  title: 'UI/Chrome/AppSidebar',
  component: AppSidebar,
  tags: ['autodocs'],
  parameters: {
    layout: 'fullscreen',
    catalogueRoute: '/admin/users',
  },
} satisfies Meta<typeof AppSidebar>

export default meta
type Story = StoryObj<typeof meta>

export const Default: Story = {
  render: () => ({
    components: { AppSidebar, SidebarProvider },
    template: `
      <SidebarProvider :default-open="true" class="flex min-h-[32rem] w-full">
        <AppSidebar />
      </SidebarProvider>
    `,
  }),
}
