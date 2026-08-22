# apps/web — EnterpriseStarter SPA

Vue 3 + TypeScript + Vite + shadcn-vue (`@/ui`) with centralized cookie and CSRF handling.

## Setup

```bash
cp .env.example .env
npm install
npm run dev
```

Prefer **`VITE_API_BASE_URL=/api/v1`** so the Vite proxy keeps auth cookies same-origin. For a standalone API on port 5000:

```bash
VITE_API_BASE_URL=/api/v1 VITE_API_PROXY_TARGET=http://localhost:5000 npm run dev
```

Avoid pointing the browser at a cross-origin absolute API URL for cookie auth.

## Progressive Web App

The production SPA is installable. It is **not** a second client: same origin, cookie session, CSRF, and `@/ui`. The service worker caches the shell only and does not treat `/api` as an SPA page.

### How to install / test

| Situation | What to run | What you should see |
|-----------|-------------|---------------------|
| Normal development | `npm run dev` (or Aspire) | No service worker. HMR as usual. |
| Local install testing | `npm run dev:pwa` | Dev service worker. Chromium shows **Install** in the address bar. **Install app** may also appear in the account menu (AD avatar) and on the sign-in screen. |
| Production-like locally | `npm run build && npm run preview` | Real `sw.js` + manifest, same as Compose. |
| Deployed nginx | Compose / `npm run build` | Installable. **HTTPS required** for phones. Safari/iOS: Share → Add to Home Screen. |

`VITE_PWA_DEV=true` is the same opt-in as `npm run dev:pwa` (see `.env.example`).

Already-installed windows (`display-mode: standalone`) hide **Install app**. Offline data, web push, and background sync are not included.

## Scripts

| Command | Description |
|---------|-------------|
| `npm run dev` | Vite dev server (+ `/api` proxy); no service worker |
| `npm run dev:pwa` | Same Vite server with a dev service worker (install prompt) |
| `npm run build` | Production build (installable PWA; `/api` never cached) |
| `npm run type-check` | `vue-tsc` |
| `npm run lint` | ESLint |
| `npm run test:unit` | Vitest |
| `npm run storybook` | Optional `@/ui` catalogue at http://localhost:6006 (not the SPA) |
| `npm run build-storybook` | Static catalogue build (`storybook-static/`) |

## API layer

| Path | Role |
|------|------|
| `src/api/base/client.ts` | `fetch`, cookie credentials, CSRF token/header |
| `src/api/base/base.ts` | `BaseApiService` CRUD helper |
| `src/api/authApi.ts` / `usersApi.ts` | Session and user APIs |
| `src/api/rolesApi.ts` / `securityAuditApi.ts` | Global RBAC and audit APIs |
| `src/api/types/schema.ts` | Types + light parsers |
| `src/stores/auth.ts` | Session, roles, permissions, required password change |

## Routes

| Path | Notes |
|------|-------|
| `/login` | Guest layout |
| `/` | Home (auth) |
| `/profile`, `/change-password` | Account management |
| `/admin/users` | Requires `users.read` |
| `/admin/roles` | Requires `roles.read` |
| `/admin/security-audit` | Requires `audit.read` |

Guards use `meta.requiresAuth`, `meta.guest`, and `meta.permission`, and enforce required password changes.

## Modules

`src/modules/registry.ts` registers the Companion web module. Product modules add routes and navigation through explicit compile-time registration.

## UI / theme (compose only)

Styling is centralized. Views/modules **compose** shared pieces; they do not invent colors or card/button looks.

| Layer | Where | What to edit |
|-------|-------|----------------|
| Tokens | `src/styles/theme.css` | Brand block: `--brand`, `--brand-foreground`, `--brand-muted` (swap these to re-skin) |
| Primitives | `src/ui/` via `@/ui` | Button/Input/Sidebar/etc. look |
| App chrome | `src/components/` | `PageBody`, `PageHeader`, `FormPanel`, `DataList*`, `StatusMessage`, shell |

- Import primitives from `@/ui` only (never deep paths from features)
- Layout: `AppShell.vue` + `AppSidebar.vue` (collapse control lives in the sidebar footer)
- Details: [`src/ui/README.md`](src/ui/README.md)
- Optional live catalogue: `npm run storybook` (http://localhost:6006). Not required to run or deploy the app.

```bash
npx shadcn-vue@latest add button
```

Config: `components.json` (`ui` alias → `@/ui`).
