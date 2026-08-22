import type { Meta, StoryObj } from '@storybook/vue3-vite'
import { Button, DataList, DataListEmpty, DataListItem } from '@/ui'

const meta = {
  title: 'UI/Chrome/DataList',
  component: DataList,
  tags: ['autodocs'],
} satisfies Meta<typeof DataList>

export default meta
type Story = StoryObj<typeof meta>

export const Items: Story = {
  render: () => ({
    components: { Button, DataList, DataListItem },
    template: `
      <DataList class="max-w-xl">
        <DataListItem title="Ada Admin" description="admin@enterprisestarter.local">
          <template #actions>
            <Button variant="outline" size="sm" shape="square">Edit</Button>
          </template>
        </DataListItem>
        <DataListItem title="Pat Member" description="pat@enterprisestarter.local">
          <template #actions>
            <Button variant="outline" size="sm" shape="square">Edit</Button>
          </template>
        </DataListItem>
      </DataList>
    `,
  }),
}

export const Empty: Story = {
  render: () => ({
    components: { DataList, DataListEmpty },
    template: `
      <DataList class="max-w-xl">
        <DataListEmpty>No users yet.</DataListEmpty>
      </DataList>
    `,
  }),
}
