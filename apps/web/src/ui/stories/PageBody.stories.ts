import type { Meta, StoryObj } from '@storybook/vue3-vite'
import { PageBody, PageHeader } from '@/ui'

const meta = {
  title: 'UI/Chrome/PageBody',
  component: PageBody,
  tags: ['autodocs'],
} satisfies Meta<typeof PageBody>

export default meta
type Story = StoryObj<typeof meta>

export const Default: Story = {
  render: () => ({
    components: { PageBody, PageHeader },
    template: `
      <PageBody>
        <PageHeader title="Users" description="Admin-created accounts." />
        <p class="text-sm text-muted-foreground">Page content uses PageBody padding and rhythm.</p>
      </PageBody>
    `,
  }),
}

export const Workbench: Story = {
  render: () => ({
    components: { PageBody, PageHeader },
    template: `
      <PageBody variant="workbench">
        <PageHeader size="compact" title="Today" description="Monday, August 24" />
        <p class="px-4 py-3 text-sm text-muted-foreground">
          Workbench fills the shell so panes scroll instead of stacking cards.
        </p>
      </PageBody>
    `,
  }),
}
