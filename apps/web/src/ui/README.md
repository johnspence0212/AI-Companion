# Template library (`@/ui`)

This is the **only** public UI surface for features (`src/views/`, `src/modules/`, `App.vue`).

Do not invent looks in features. Do not deep-import. Do not restyle primitives from a view.

Browse the live catalogue (optional — not required to run the SPA):

```bash
cd apps/web
npm run storybook
```

Open http://localhost:6006. Stories import from `@/ui` the same way features do. Add a story when you add a primitive or chrome piece.

## Styling contract (three layers)

| Layer          | Location                                              | Owns                                                          |
| -------------- | ----------------------------------------------------- | ------------------------------------------------------------- |
| 1. Tokens      | [`../styles/theme.css`](../styles/theme.css)          | Brand + semantic colors / radius — edit `--brand*` to re-skin |
| 2a. Primitives | This folder (`src/ui/{name}/`)                        | Component look (variants, focus, sizes)                       |
| 2b. App chrome | [`../components/`](../components/) (re-exported here) | Shell, page body, lists, form panels, status text             |
| 3. Features    | `src/views/`, `src/modules/`                          | Compose `@/ui` only — **no invented styles**                  |

**Forbidden in features (enforced by ESLint):**

- Importing `@/components/*` or deep `@/ui/button` paths
- Hex / arbitrary colors (`bg-[#…]`, `#1b4332`)
- Inline `style=` attributes
- Calling `fetch` directly (use `@/api` / stores)
- Restyling primitives (`class="bg-green-700"` on `Button`)

**Need a new look?** Add or extend a shared component / token — never style in the view.

## How to add something

### Primitive (button, dialog, select, …)

```bash
cd apps/web
npx shadcn-vue@latest add dialog
```

1. Lands under `src/ui/{name}/`
2. Re-export from [`index.ts`](./index.ts)
3. Features: `import { Dialog, DialogContent } from '@/ui'`
4. Add `src/ui/{name}/{Name}.stories.ts` that imports from `@/ui`
5. Library internals may deep-import (`@/ui/button`) to avoid circular barrels

Skill: `.cursor/skills/add-shadcn-component/`

### App chrome / pattern (new list shell, status tone, …)

1. Implement under `src/components/YourPattern.vue`
2. Export from [`chrome.ts`](./chrome.ts) and [`index.ts`](./index.ts)
3. Document in the tables below
4. Add a story under `src/ui/stories/` that imports from `@/ui`
5. Features import from `@/ui` only

## Public primitives

Button, Input, Label, Separator, Dialog (+ parts), Sheet (+ parts), Sidebar (+ common parts), Skeleton, Tooltip (+ parts).

Button shapes: labeled actions are `shape="square"` (`rounded-md`) by default. Icon sizes (`icon`, `icon-sm`, `icon-lg`) stay circular. Use `variant="outline"` for secondary actions (Cancel, Close, Archive) and the default fill for the primary action (Save, Add Note).

## Brand colors

Edit the **Brand** block in [`../styles/theme.css`](../styles/theme.css) (`--brand`, `--brand-foreground`, `--brand-muted`). Semantic tokens (`--primary`, `--sidebar*`, `--ring`) are wired to brand. Do not hardcode brand colors in components or modules.

## App chrome (compose these in views)

| Component                                             | Role                                                           |
| ----------------------------------------------------- | -------------------------------------------------------------- |
| `PageBody`                                            | `page` padding or `workbench` fill layout                      |
| `PageHeader`                                          | Eyebrow, title, description, top-right actions                 |
| `StatusMessage`                                       | Error / muted / success text                                   |
| `FormPanel`                                           | Auth/profile card form shell                                   |
| `FormSlideout` / `FormSection` / `FormField`          | Create/edit Sheet forms                                        |
| `MultiSelect`                                         | Multi-value selector for forms                                 |
| `DataList` / `DataListItem` / `DataListEmpty`         | Admin list panels                                              |
| `DataTable`                                           | Dense bordered tables                                          |
| `WorkbenchPanes`                                      | Connected panes (nav / list / detail), not cards               |
| `WorkbenchComposer`                                   | Compact capture/add/search bar inside a pane                   |
| `WorkbenchSearch`                                     | Centered top-bar search that opens a centered overlay          |
| `WorkbenchInbox`                                      | Top-bar Inbox button that opens a centered overlay             |
| `NotesTree`                                           | Nested notes list; adding a note inside another nests it       |
| `WorkbenchSection`                                    | Named group inside a pane (Carryover, results)                 |
| `MarkdownSource`                                      | Preview default; Edit opens Source. Library Edit becomes Save. |
| `SavedViewBar`                                        | System/user Saved View picker inside destinations              |
| `SurfaceCard`                                         | Simple linked/static surface cards                             |
| `GuestShell` / `AppShell` / `AppSidebar` / `UserMenu` | Layout shells                                                  |

## Approved recipes

### List page (create / edit)

1. Page is list-only inside `PageBody` + `PageHeader` (optional `eyebrow`).
2. Primary action opens `FormSlideout` in create mode.
3. Row **Edit** opens the same slideout prefilled.
4. Pass `allowFullscreen` only when the screen needs a full-screen toggle (default off).

### Guest / auth form

`GuestShell` → `FormPanel` → `FormField` / `Input` / `Button` + `StatusMessage`.

### Dense audit / table

`PageBody` → `PageHeader` → `DataTable`.

### Workbench destination

`PageBody variant="workbench"` → `WorkbenchPanes` filling the remaining height. The shell breadcrumb is the destination name; do not repeat it as a page hero. Projects is three-pane. Library is two-pane: `NotesTree` plus a flush `MarkdownSource` reading pane (Preview default; Edit opens Source and becomes Save). Adding a note inside a note nests it; the parent note still has its own body. Today is `layout="home"` (Daily + Carryover/Blocked). Search and Inbox live in the top bar (`WorkbenchSearch`, `WorkbenchInbox`) and open centered overlays — they are not rail destinations. `SavedViewBar` is a compact toolbar, not a second list. Open Project Context, Issue, Inbox, or Search Document hits in `FormSlideout` with `allowFullscreen` and `MarkdownSource`. Search Document hits use the same Preview / Edit / Cancel / Save controls as Library.

If a screen doesn’t match a recipe, extend the library — don’t invent a one-off layout in the view.

## Replace / remove shadcn

1. Keep the public exports in `index.ts` stable (or map them once).
2. Swap implementations under this folder / `src/components/`.
3. Update shell chrome if layout primitives change.
4. Module/feature code under `src/views/` and `src/modules/` should not need edits.
