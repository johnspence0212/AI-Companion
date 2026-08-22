import type { Meta, StoryObj } from '@storybook/vue3-vite'
import { Button, Tooltip, TooltipContent, TooltipProvider, TooltipTrigger } from '@/ui'

const meta = {
  title: 'UI/Primitives/Tooltip',
  component: Tooltip,
  tags: ['autodocs'],
} satisfies Meta<typeof Tooltip>

export default meta
type Story = StoryObj<typeof meta>

export const Default: Story = {
  render: () => ({
    components: { Button, Tooltip, TooltipContent, TooltipProvider, TooltipTrigger },
    template: `
      <TooltipProvider>
        <Tooltip>
          <TooltipTrigger as-child>
            <Button variant="outline">Hover</Button>
          </TooltipTrigger>
          <TooltipContent>Administration</TooltipContent>
        </Tooltip>
      </TooltipProvider>
    `,
  }),
}
