import type { Meta, StoryObj } from '@storybook/vue3-vite'
import { AppShell, PageBody, PageHeader } from '@/ui'

const meta = {
  title: 'UI/Chrome/AppShell',
  component: AppShell,
  tags: ['autodocs'],
  parameters: {
    layout: 'fullscreen',
    catalogueRoute: '/admin/users',
  },
} satisfies Meta<typeof AppShell>

export default meta
type Story = StoryObj<typeof meta>

export const Users: Story = {
  render: () => ({
    components: { AppShell, PageBody, PageHeader },
    template: `
      <AppShell>
        <PageBody>
          <PageHeader
            eyebrow="Settings"
            title="Users"
            description="Catalogue preview — no API calls."
          />
        </PageBody>
      </AppShell>
    `,
  }),
}

export const Home: Story = {
  parameters: {
    catalogueRoute: '/',
  },
  render: () => ({
    components: { AppShell, PageBody, PageHeader },
    template: `
      <AppShell>
        <PageBody>
          <PageHeader title="Home" description="Authenticated landing chrome." />
        </PageBody>
      </AppShell>
    `,
  }),
}
