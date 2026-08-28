import type { Meta, StoryObj } from '@storybook/vue3-vite'
import {
  Button,
  DataList,
  DataListEmpty,
  DataListItem,
  SavedViewBar,
  StatusMessage,
  WorkbenchComposer,
  WorkbenchPanes,
  WorkbenchSection,
} from '@/ui'
import { ref } from 'vue'

const meta = {
  title: 'UI/Chrome/WorkbenchPanes',
  component: WorkbenchPanes,
  tags: ['autodocs'],
  parameters: {
    layout: 'fullscreen',
  },
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
    components: {
      Button,
      DataList,
      DataListItem,
      SavedViewBar,
      StatusMessage,
      WorkbenchComposer,
      WorkbenchPanes,
    },
    setup() {
      const folder = ref('')
      const selectedId = ref('system')
      const views = [
        { id: 'system', name: 'All Documents', isSystem: true },
        { id: 'copy', name: 'Reading list', isSystem: false },
      ]
      return { args, folder, selectedId, views }
    },
    template: `
      <div class="flex h-[32rem] flex-col">
        <WorkbenchPanes v-bind="args">
          <template #nav>
            <WorkbenchComposer v-model="folder" placeholder="New folder" submit-label="Add" />
            <DataList variant="flush">
              <DataListItem title="All" selected interactive />
              <DataListItem title="Unfiled" interactive />
            </DataList>
          </template>
          <template #list-actions>
            <Button size="sm" shape="square">New</Button>
          </template>
          <template #list-toolbar>
            <SavedViewBar :views="views" :selected-id="selectedId" @select="selectedId = $event" />
          </template>
          <template #list>
            <DataList variant="flush">
              <DataListItem title="Architecture notes" description="Updated today" selected interactive />
            </DataList>
          </template>
          <template #detail>
            <div class="p-3">
              <StatusMessage>Preview is the default. Source Markdown stays exact.</StatusMessage>
            </div>
          </template>
        </WorkbenchPanes>
      </div>
    `,
  }),
}

export const Home: Story = {
  args: {
    listTitle: 'Daily',
    detailTitle: 'Waiting',
    layout: 'home',
  },
  render: (args) => ({
    components: {
      DataList,
      DataListEmpty,
      DataListItem,
      WorkbenchComposer,
      WorkbenchPanes,
      WorkbenchSection,
    },
    setup() {
      const draft = ref('')
      return { args, draft }
    },
    template: `
      <div class="flex h-[32rem] flex-col">
        <WorkbenchPanes v-bind="args">
          <template #list-toolbar>
            <WorkbenchComposer v-model="draft" placeholder="Add a custom Daily Item" />
          </template>
          <template #list>
            <DataList variant="flush">
              <DataListItem title="Review Project Context" description="Custom Daily Item" />
            </DataList>
          </template>
          <template #detail>
            <WorkbenchSection title="Carryover">
              <DataList variant="flush">
                <DataListEmpty>Incomplete items stay on their original dates.</DataListEmpty>
              </DataList>
            </WorkbenchSection>
            <WorkbenchSection title="Blocked / Waiting">
              <DataList variant="flush">
                <DataListEmpty>Nothing blocked.</DataListEmpty>
              </DataList>
            </WorkbenchSection>
          </template>
        </WorkbenchPanes>
      </div>
    `,
  }),
}
