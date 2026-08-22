# EnterpriseStarter file index

Key files for the PostgreSQL, cookie/CSRF, global-RBAC modular monolith.

## Solution & orchestration

| File | Role |
|------|------|
| `EnterpriseStarter.sln` | All backend, test, and Aspire projects |
| `EnterpriseStarter.code-workspace` | Multi-root VS Code / Cursor workspace |
| `aspire/AppHost/Program.cs` | PostgreSQL + API + Vite; injects `enterprisestarterdb` |
| `aspire/ServiceDefaults/Extensions.cs` | OpenTelemetry, health, service discovery |
| `Directory.Build.props` | Shared MSBuild properties |

## API host (`apps/api`)

| File | Role |
|------|------|
| `Program.cs` | Thin composition root and optional migration execution |
| `EnterpriseStarter.Api.csproj` | References platform, abstractions, ServiceDefaults |
| `appsettings.json` | PostgreSQL, cookie, seed, CORS, and rate limits |
| `Dockerfile` | Shared API/migrator image |

## Platform (`apps/platform`)

| File | Role |
|------|------|
| `PlatformExtensions.cs` | DI, Identity, PostgreSQL, CSRF, rate limits, pipeline, seed |
| `Controllers.cs` | `/api/v1` auth, users, roles, and audit controllers |
| `Domain.cs` | users, roles, permissions, options, audit entity |
| `Security.cs` | permission handler/claims and persistent security audit service |
| `Data.cs` | `EnterpriseDbContext` and design-time factory |
| `Migrations/` | Shared PostgreSQL migration stream |

## Module abstractions (`apps/module-abstractions`)

| File | Role |
|------|------|
| `IEnterpriseModule.cs` | Module contract, permissions, empty production registry |

## API tests

| File | Role |
|------|------|
| `apps/api.tests/ApiIntegrationTests.cs` | Cookie/CSRF, lifecycle, global RBAC, audit, registration absence |
| `apps/api.tests/ModuleExtensionTests.cs` | Compile-time module extension and empty production registry |
| `apps/api.tests/DeployHardeningTests.cs` | Security/correlation headers |
| `apps/api.tests/CustomWebApplicationFactory.cs` | Test host factory |
| `apps/test-module/` | Test-only module proving extension seams |

## Web (`apps/web`)

| File | Role |
|------|------|
| `src/main.ts` | Vue app entry, Pinia, router, auth hydrate |
| `src/styles/theme.css` | Brand colors / radius (edit here) |
| `src/styles/index.css` | Tailwind + theme import |
| `src/ui/` | Template library primitives |
| `src/ui/index.ts` | Public UI barrel (primitives + chrome) — **only** import surface for features |
| `src/ui/chrome.ts` | Re-exports app chrome from `src/components/` |
| `src/ui/README.md` | Library contract, recipes, add paths, Storybook catalogue |
| `src/ui/**/*.stories.ts` | Optional Storybook examples (import from `@/ui`) |
| `.storybook/` | Storybook 10 Vue 3 + Vite config (port 6006; not the SPA) |
| `src/components/` | Chrome implementations (consume via `@/ui`, not directly) |
| `src/components/AppShell.vue` | Authenticated layout chrome |
| `src/components/AppSidebar.vue` | Sidebar nav (permission/module gated) |
| `src/router/index.ts` | Auth, forced-password-change, permission, module routes |
| `src/stores/auth.ts` | Cookie session, permissions, forced password change |
| `src/api/base/client.ts` | `fetch`, cookie credentials, CSRF token handling |
| `src/api/authApi.ts` | Auth endpoints |
| `src/api/usersApi.ts` | User admin API |
| `src/api/rolesApi.ts` | Global roles and permission catalog |
| `src/api/securityAuditApi.ts` | Security audit API |
| `src/api/types/schema.ts` | Types + light response parsers |
| `src/modules/registry.ts` | Empty compile-time product-module registry |
| `src/views/LoginView.vue` | Login page |
| `src/views/HomeView.vue` | Authenticated home shell |
| `src/views/UsersView.vue` | Global user management |
| `src/views/RolesView.vue` | Global role management |
| `src/views/SecurityAuditView.vue` | Security audit log |
| `src/views/ChangePasswordView.vue` | Required password-change flow |
| `src/views/ProfileView.vue` | Current-user profile |
| `src/lib/pwaPolicy.ts` | PWA theme color (keep in sync with `--brand`); `/api` is not a SPA fallback |
| `src/lib/pwaInstall.ts` | `beforeinstallprompt` helper for the Install app action |
| `vite.config.ts` | Vite + same-origin `/api` proxy; PWA in production builds; `npm run dev:pwa` opts in for Vite |
| `public/pwa-192.png` / `pwa-512.png` | Install icons |
| `nginx.conf` | SPA fallback, `/api` proxy, SW/manifest no-cache, CSP `worker-src` |

## E2E & CI

| File | Role |
|------|------|
| `e2e/playwright.config.ts` | Starts api + web (Postgres), runs tests |
| `e2e/tests/auth.spec.ts` | Login and forced-password-change flow |
| `.github/workflows/ci.yml` | api / web / e2e CI jobs |

## Docker (optional deploy)

| File | Role |
|------|------|
| `docker-compose.yml` | postgres + migrator + api + nginx web |
| `.env.docker.example` | Secrets, cookie policy, optional OTEL |
| `.cursor/docs/DEPLOY-RUNBOOK.md` | Reference deployment, seed, backup, TLS, OTEL |
| `apps/api/Dockerfile` | Multi-stage API image |
| `apps/web/Dockerfile` | Vite build → nginx |
| `apps/web/nginx.conf` | SPA routing, `/api` proxy, PWA SW/manifest, security headers |

## Cursor config

| Path | Role |
|------|------|
| `.cursor/rules/` | Agent rules (always + glob-scoped) |
| `.cursor/skills/` | Task-specific agent skills |
| `.cursor/docs/FILE-INDEX.md` | This file |
| `.cursor/docs/ENTERPRISE-PLAN.md` | Implemented scope and extension decisions |
| `.cursor/docs/ARCHITECTURE.md` | Architecture and security overview |
| `.cursor/docs/adr/` | Concise architecture decisions |
| `.cursor/docs/DEPLOY-RUNBOOK.md` | Compose deploy ops |
| `.cursor/docs/PHASE-6-BACKLOG.md` | Optional product extensions |
