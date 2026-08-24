import type { Meta, StoryObj } from '@storybook/vue3-vite'
import { DataList, DataListEmpty, DataListItem, WorkbenchSection } from '@/ui'

const meta = {
  title: 'UI/Chrome/WorkbenchSection',
  component: WorkbenchSection,
  tags: ['autodocs'],
} satisfies Meta<typeof WorkbenchSection>

export default meta
type Story = StoryObj<typeof meta>

export const Carryover: Story = {
  args: {
    title: 'Carryover',
  },
  render: (args) => ({
    components: { DataList, DataListEmpty, DataListItem, WorkbenchSection },
    setup: () => ({ args }),
    template: `
      <WorkbenchSection v-bind="args">
        <DataList variant="flush">
          <DataListEmpty>Incomplete items stay on their original dates.</DataListEmpty>
        </DataList>
      </WorkbenchSection>
    `,
  }),
}
