import type { Meta, StoryObj } from '@storybook/vue3-vite'
import { ref } from 'vue'
import { NotesTree } from '@/ui'
import type { NotesTreeNode } from '@/lib/notesTree'

const items: NotesTreeNode[] = [
  {
    id: 'reading',
    title: 'Reading list',
    children: [{ id: 'chapter', title: 'Chapter one', children: [] }],
  },
  { id: 'loose', title: 'Loose note', children: [] },
]

const meta = {
  title: 'UI/Chrome/NotesTree',
  component: NotesTree,
  tags: ['autodocs'],
} satisfies Meta<typeof NotesTree>

export default meta
type Story = StoryObj<typeof meta>

export const Nested: Story = {
  args: {
    items,
    expandedIds: ['reading'],
    selectedId: 'chapter',
  },
  render: (args) => ({
    components: { NotesTree },
    setup() {
      const selectedId = ref(args.selectedId ?? null)
      const expandedIds = ref([...args.expandedIds])
      function toggle(id: string) {
        expandedIds.value = expandedIds.value.includes(id)
          ? expandedIds.value.filter((item) => item !== id)
          : [...expandedIds.value, id]
      }
      return { args, selectedId, expandedIds, toggle }
    },
    template: `
      <div class="max-w-sm border">
        <NotesTree
          :items="args.items"
          :selected-id="selectedId"
          :expanded-ids="expandedIds"
          @select="selectedId = $event"
          @toggle="toggle"
        />
      </div>
    `,
  }),
}
