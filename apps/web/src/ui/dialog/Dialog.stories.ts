import type { Meta, StoryObj } from '@storybook/vue3-vite'
import { Button, Dialog, DialogContent, DialogDescription, DialogHeader, DialogTitle } from '@/ui'

const meta = {
  title: 'UI/Primitives/Dialog',
  component: Dialog,
  tags: ['autodocs'],
} satisfies Meta<typeof Dialog>

export default meta
type Story = StoryObj<typeof meta>

export const Default: Story = {
  render: () => ({
    components: { Button, Dialog, DialogContent, DialogDescription, DialogHeader, DialogTitle },
    template: `
      <Dialog default-open>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Search</DialogTitle>
            <DialogDescription>Grouped results over Projects, Documents, Issues, and Activity.</DialogDescription>
          </DialogHeader>
        </DialogContent>
      </Dialog>
    `,
  }),
}
