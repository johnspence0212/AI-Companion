# App chrome (implementations)

Vue implementations for page shells, lists, and forms live here.

**Do not import this folder from features.** The public template library is [`@/ui`](../ui/README.md) (`src/ui/index.ts` re-exports these components).

| Need                        | Do this                                                                                                             |
| --------------------------- | ------------------------------------------------------------------------------------------------------------------- |
| Use chrome in a view/module | `import { PageBody, WorkbenchPanes, WorkbenchComposer, WorkbenchSearch, MarkdownSource, SavedViewBar } from '@/ui'` |
| Change a look               | Edit the component here and/or tokens in `src/styles/theme.css`                                                     |
| Add a new pattern           | Add a component here, export it from `src/ui/chrome.ts` + `src/ui/index.ts`, document in `src/ui/README.md`         |

Library internals may import other chrome via relative paths (`./PageBody.vue`) and primitives via deep paths (`@/ui/button`) to avoid circular barrels.
