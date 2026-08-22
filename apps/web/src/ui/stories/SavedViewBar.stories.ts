import type { Meta, StoryObj } from '@storybook/vue3-vite'
import { ref } from 'vue'
import { SavedViewBar } from '@/ui'

const meta = {
  title: 'UI/Chrome/SavedViewBar',
  component: SavedViewBar,
  tags: ['autodocs'],
} satisfies Meta<typeof SavedViewBar>

export default meta
type Story = StoryObj<typeof meta>

export const SystemAndUser: Story = {
  args: {
    views: [],
    selectedId: null,
  },
  render: () => ({
    components: { SavedViewBar },
    setup() {
      const selectedId = ref('system')
      const views = [
        { id: 'system', name: 'All Documents', isSystem: true },
        { id: 'copy', name: 'Reading list', isSystem: false },
      ]
      return { selectedId, views }
    },
    template: `
      <SavedViewBar
        :views="views"
        :selected-id="selectedId"
        @select="selectedId = $event"
        @duplicate="selectedId = 'copy'"
      />
    `,
  }),
}
