---
name: add-shadcn-component
description: Adds UI to the EnterpriseStarter template library — shadcn-vue primitives or app chrome patterns — and wires the public @/ui barrel. Use when adding buttons, dialogs, forms, tables, page shells, or other shared UI.
---

# Add to the template library (`@/ui`)

EnterpriseStarter has **one** public UI surface: `import { … } from '@/ui'`.

Features (`src/views/`, `src/modules/`, `App.vue`) must never:

- Deep-import `@/ui/button` or `@/components/PageBody.vue`
- Invent colors, cards, or layouts in the view
- Call `fetch` directly

Need a new look? Extend the library (this skill), not the feature.

## Prerequisites

```bash
cd apps/web
npm install   # if needed
```

`components.json` uses **new-york** style, Tailwind 4, and `"ui": "@/ui"`.

---

## Path A — shadcn primitive

```bash
cd apps/web
npx shadcn-vue@latest add button
npx shadcn-vue@latest add dialog
```

1. Component lands under `src/ui/{name}/`
2. **Required:** re-export from `src/ui/index.ts`
3. Features use the barrel only:

```vue
<script setup lang="ts">
import { Button, Dialog, DialogContent } from '@/ui'
</script>
```

4. Library internals (`src/ui/**`, `src/components/**`) may deep-import (`@/ui/button`) to avoid circular barrels
5. Brand colors stay in `src/styles/theme.css` (`--brand*`)
6. Icons: `lucide-vue-next`
7. Class merging: `cn()` from `@/lib/utils`
8. Add `src/ui/{name}/{Name}.stories.ts` importing from `@/ui` (optional Storybook catalogue)

---

## Path B — app chrome / pattern

Use when the need is a **page recipe** (list shell, form slideout section, status tone), not a raw primitive.

1. Implement `src/components/YourPattern.vue`
2. Export from `src/ui/chrome.ts` and `src/ui/index.ts`
3. Document in `src/ui/README.md` (chrome table + recipe if new)
4. Features: `import { YourPattern } from '@/ui'`
5. Add `src/ui/stories/YourPattern.stories.ts` importing from `@/ui`

Do **not** leave a new pattern importable only via `@/components/...`.

---

## Approved recipes (prefer these)

| Recipe | Compose |
|--------|---------|
| List + create/edit | `PageBody` + `PageHeader` + `DataList*` + `FormSlideout` |
| Guest auth form | `GuestShell` + `FormPanel` + fields + `StatusMessage` |
| Dense table | `PageBody` + `PageHeader` + `DataTable` |

If the screen doesn’t fit, extend the library first.

---

## Verify

```bash
cd apps/web
npm run type-check
npm run lint
npm run storybook   # optional catalogue at http://localhost:6006
```

ESLint fails features that deep-import UI, use hex/arbitrary colors, inline styles, or call `fetch`.

Browse [shadcn-vue docs](https://www.shadcn-vue.com/docs/components) for available primitives.
