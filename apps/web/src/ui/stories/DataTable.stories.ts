import type { Meta, StoryObj } from '@storybook/vue3-vite'
import { DataTable } from '@/ui'

const meta = {
  title: 'UI/Chrome/DataTable',
  component: DataTable,
  tags: ['autodocs'],
} satisfies Meta<typeof DataTable>

export default meta
type Story = StoryObj<typeof meta>

export const Default: Story = {
  render: () => ({
    components: { DataTable },
    template: `
      <DataTable>
        <thead>
          <tr class="border-b text-muted-foreground">
            <th class="px-4 py-3 font-medium">When</th>
            <th class="px-4 py-3 font-medium">Event</th>
            <th class="px-4 py-3 font-medium">Actor</th>
          </tr>
        </thead>
        <tbody>
          <tr class="border-b">
            <td class="px-4 py-3">2026-08-12 21:55</td>
            <td class="px-4 py-3">login.succeeded</td>
            <td class="px-4 py-3">admin@enterprisestarter.local</td>
          </tr>
          <tr>
            <td class="px-4 py-3">2026-08-12 21:56</td>
            <td class="px-4 py-3">user.created</td>
            <td class="px-4 py-3">admin@enterprisestarter.local</td>
          </tr>
        </tbody>
      </DataTable>
    `,
  }),
}
