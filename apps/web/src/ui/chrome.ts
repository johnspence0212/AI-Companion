/**
 * App chrome — page shells, lists, forms, status.
 * Implementations live in `src/components/`; the public API is `@/ui` only.
 */
export { default as AppShell } from '../components/AppShell.vue'
export { default as AppSidebar } from '../components/AppSidebar.vue'
export { default as GuestShell } from '../components/GuestShell.vue'
export { default as UserMenu } from '../components/UserMenu.vue'

export { default as PageBody } from '../components/PageBody.vue'
export { default as PageHeader } from '../components/PageHeader.vue'
export { default as StatusMessage } from '../components/StatusMessage.vue'
export { default as SurfaceCard } from '../components/SurfaceCard.vue'

export { default as FormPanel } from '../components/FormPanel.vue'
export { default as FormSlideout } from '../components/FormSlideout.vue'
export { default as FormSection } from '../components/FormSection.vue'
export { default as FormField } from '../components/FormField.vue'
export { default as MultiSelect } from '../components/MultiSelect.vue'
export type { MultiSelectOption } from '../components/MultiSelect.vue'

export { default as DataList } from '../components/DataList.vue'
export { default as DataListItem } from '../components/DataListItem.vue'
export { default as DataListEmpty } from '../components/DataListEmpty.vue'
export { default as DataTable } from '../components/DataTable.vue'
export { default as WorkbenchPanes } from '../components/WorkbenchPanes.vue'
export { default as MarkdownSource } from '../components/MarkdownSource.vue'
