import type { Meta, StoryObj } from '@storybook/vue3-vite'
import {
  Button,
  Sheet,
  SheetContent,
  SheetDescription,
  SheetFooter,
  SheetHeader,
  SheetTitle,
  SheetTrigger,
} from '@/ui'

const meta = {
  title: 'UI/Primitives/Sheet',
  component: Sheet,
  tags: ['autodocs'],
} satisfies Meta<typeof Sheet>

export default meta
type Story = StoryObj<typeof meta>

export const Default: Story = {
  render: () => ({
    components: {
      Button,
      Sheet,
      SheetContent,
      SheetDescription,
      SheetFooter,
      SheetHeader,
      SheetTitle,
      SheetTrigger,
    },
    template: `
      <Sheet>
        <SheetTrigger as-child>
          <Button shape="square">Open sheet</Button>
        </SheetTrigger>
        <SheetContent>
          <SheetHeader>
            <SheetTitle>Edit role</SheetTitle>
            <SheetDescription>Code-defined permissions stay in the API.</SheetDescription>
          </SheetHeader>
          <SheetFooter>
            <Button variant="outline" shape="square">Cancel</Button>
            <Button shape="square">Save</Button>
          </SheetFooter>
        </SheetContent>
      </Sheet>
    `,
  }),
}
