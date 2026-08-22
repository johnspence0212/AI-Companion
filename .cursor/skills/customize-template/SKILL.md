---
name: customize-template
description: Customizes EnterpriseStarter branding, bootstrap admin, CORS, OpenAPI, theme, navigation, and compile-time modules.
---

# Customize EnterpriseStarter

See [reference.md](reference.md) for all touch points.

## Quick wins

| What | Where |
|------|-------|
| Seed admin | `Seed:AdminEmail`, `Seed:AdminPassword` |
| Cookie name | `Auth:CookieName` |
| Cookie secure policy | `Auth:CookieSecurePolicy` |
| CORS | `WebOrigin`, `WebOrigins` |
| Web package name | `apps/web/package.json` → `name` |
| Page title | `apps/web/index.html` |
| Brand colors | `apps/web/src/styles/theme.css` Brand block (`--brand`, `--brand-foreground`, `--brand-muted`) **only** |
| PWA name / theme | `VITE_APP_NAME`; theme color in `apps/web/src/lib/pwaPolicy.ts` (keep in sync with `--brand`); icons in `apps/web/public/pwa-*.png` |

## Secrets (deploy)

Set `POSTGRES_PASSWORD` and a unique `SEED_ADMIN_PASSWORD` in root `.env` (from `.env.docker.example`). There is no `JWT_SECRET` in the cookie model.

## Navigation / branding

- Sidebar (permission/module gated): `apps/web/src/components/AppSidebar.vue`
- Shell: `apps/web/src/components/AppShell.vue`
- Home: `apps/web/src/views/HomeView.vue`
- Login/profile/password: `apps/web/src/views/`

## Modules

Add product modules by implementing `IEnterpriseModule` and registering them in the backend and web compile-time registries. The production registries are empty by default.

Do not commit real secrets.
