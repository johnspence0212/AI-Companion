/**
 * EnterpriseStarter template library — the only public UI surface for features.
 *
 * - Primitives: shadcn-vue under `src/ui/*`
 * - Chrome: app patterns under `src/components/*`, re-exported here
 *
 * Features (`src/views/`, `src/modules/`) must import from `@/ui` only.
 * Library internals may deep-import (`@/ui/button`) to avoid circular barrels.
 */

// Primitives
export { Button, buttonVariants, type ButtonVariants } from './button'
export { Input } from './input'
export { Textarea } from './textarea'
export { Label } from './label'
export { Separator } from './separator'
export {
  Sheet,
  SheetClose,
  SheetContent,
  SheetDescription,
  SheetFooter,
  SheetHeader,
  SheetTitle,
  SheetTrigger,
} from './sheet'
export {
  Sidebar,
  SidebarContent,
  SidebarFooter,
  SidebarGroup,
  SidebarGroupContent,
  SidebarGroupLabel,
  SidebarHeader,
  SidebarMenu,
  SidebarMenuButton,
  SidebarMenuItem,
  SidebarProvider,
  SidebarInset,
  SidebarTrigger,
  useSidebar,
} from './sidebar'
export { Skeleton } from './skeleton'
export { Tooltip, TooltipContent, TooltipProvider, TooltipTrigger } from './tooltip'

// App chrome
export {
  AppShell,
  AppSidebar,
  GuestShell,
  UserMenu,
  PageBody,
  PageHeader,
  StatusMessage,
  SurfaceCard,
  FormPanel,
  FormSlideout,
  FormSection,
  FormField,
  MultiSelect,
  type MultiSelectOption,
  DataList,
  DataListItem,
  DataListEmpty,
  DataTable,
  WorkbenchPanes,
  MarkdownSource,
} from './chrome'
