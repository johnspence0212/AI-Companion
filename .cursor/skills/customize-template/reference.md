# Customization Reference

## Configuration files

| File | Keys |
|------|------|
| `apps/api/appsettings.json` | `Database`, `Auth`, `Seed`, `WebOrigin`, `WebOrigins`, `ForwardedHeaders` |
| `apps/api/appsettings.Development.json` | Dev overrides |
| `.env.docker.example` | Compose secrets, cookie policy, OTEL |
| `apps/web/.env.example` | `VITE_API_BASE_URL=/api/v1`, app name |
| `apps/web/components.json` | shadcn style, aliases (`@/ui`) |

## Frontend entry

| File | Customize |
|------|-----------|
| `apps/web/src/main.ts` | Plugins, auth hydrate |
| `apps/web/src/App.vue` | Guest vs AppShell switch |
| `apps/web/src/components/AppShell.vue` | Authenticated layout chrome |
| `apps/web/src/styles/theme.css` | Brand block (`--brand*`) + semantic tokens / radius (only place for color) |
| `apps/web/src/lib/pwaPolicy.ts` | PWA theme/background (keep theme in sync with `--brand`) |
| `apps/web/public/pwa-192.png` / `pwa-512.png` | Install icons |
| `apps/web/src/router/index.ts` | Routes, auth/permission/module guards |
| `apps/web/src/components/AppSidebar.vue` | Navigation items |

## Backend platform

| File | Customize |
|------|-----------|
| `apps/platform/Domain.cs` | Auth/seed options, global roles and permissions |
| `apps/platform/PlatformExtensions.cs` | Cookie, CSRF, seed, and platform wiring |
| `apps/module-abstractions/IEnterpriseModule.cs` | Production module registry |
| `apps/web/src/modules/registry.ts` | Web module registry |

## Aspire

| File | Customize |
|------|-----------|
| `aspire/AppHost/Program.cs` | Service names, env injection (`VITE_API_BASE_URL`) |

## Solution

| File | Customize |
|------|-----------|
| `EnterpriseStarter.sln` | Solution name |
| `Directory.Build.props` | Shared analyzers / TreatWarningsAsErrors |
