import type { Meta, StoryObj } from '@storybook/vue3-vite'
import { DataList, DataListItem, StatusMessage, WorkbenchPanes } from '@/ui'

const meta = {
  title: 'UI/Chrome/WorkbenchPanes',
  component: WorkbenchPanes,
  tags: ['autodocs'],
} satisfies Meta<typeof WorkbenchPanes>

export default meta
type Story = StoryObj<typeof meta>

export const ThreePane: Story = {
  args: {
    systemView: 'All Documents',
    navTitle: 'Folders',
    listTitle: 'Documents',
    detailTitle: 'Source',
  },
  render: (args) => ({
    components: { DataList, DataListItem, StatusMessage, WorkbenchPanes },
    setup() {
      return { args }
    },
    template: `
      <WorkbenchPanes v-bind="args">
        <template #nav>
          <DataList>
            <DataListItem title="All" selected interactive />
            <DataListItem title="Unfiled" interactive />
          </DataList>
        </template>
        <template #list>
          <DataList>
            <DataListItem title="Architecture notes" description="Updated today" selected interactive />
          </DataList>
        </template>
        <template #detail>
          <StatusMessage>Source Markdown stays exact. Preview is a toggle.</StatusMessage>
        </template>
      </WorkbenchPanes>
    `,
  }),
}
