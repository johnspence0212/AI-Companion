# EnterpriseStarter architecture

## Runtime

```text
Browser
  └─ same-origin /api/v1
       └─ API host (apps/api)
            ├─ platform (apps/platform)
            ├─ compile-time modules (none in production registry)
            └─ PostgreSQL

Development: Aspire → PostgreSQL + API + Vite
Reference deployment: nginx → API; PostgreSQL + one-shot migrator
```

## Backend boundaries

- `apps/api` is the composition root. `Program.cs` selects `ModuleRegistry.Production`, adds the platform, optionally initializes the database, and maps endpoints.
- `apps/platform` owns Identity, cookie/CSRF security, global RBAC, users, roles, security audit, EF Core, and the shared migrations.
- `apps/module-abstractions` defines `IEnterpriseModule`, module permissions, and the explicit production registry.
- A product module is a compile-time dependency. It contributes services, routes, permissions, and EF model configuration; there is no runtime package scanning or enablement database.

## Web boundary

`apps/web` uses Vue 3, Pinia, Vue Router, and a shared `fetch` client. The API base normalizes to `/api/v1`; cookie credentials and CSRF handling are centralized in `src/api/base/client.ts`. Product routes and navigation are aggregated from the empty compile-time registry in `src/modules/registry.ts`.

Production builds (`npm run build` / nginx) are installable as a PWA of **the same SPA**: same origin, cookie session, and CSRF. Default `npm run dev` and Aspire do not register a service worker; `npm run dev:pwa` opts in for local install testing. The service worker caches the application shell only and never treats `/api` as an SPA navigation.

## Security model

Identity authenticates users and issues `enterprise_starter_auth`, an HttpOnly, SameSite=Lax cookie. `GET /api/v1/auth/csrf` returns a request token. Authenticated non-safe methods require that token in `X-CSRF-TOKEN`.

Authorization is global to the application. Identity roles contain `permission` claims. The protected `Admin` role receives every registered permission and `Member` starts empty. Policies evaluate permission claims; user, role, and audit administration are not tenant-scoped.

Admins create users with temporary passwords. New and reset users have `MustChangePassword=true`; the web router restricts them to the password-change flow. Important authentication and administrative actions are persisted as `SecurityAuditEvent` records.

## Data and migrations

EnterpriseStarter supports PostgreSQL only. `EnterpriseDbContext` and migrations live in `apps/platform`. Modules that contribute entities join this context and the same ordered migration stream. Aspire injects `ConnectionStrings:enterprisestarterdb`; standalone and Compose paths can use `Database:ConnectionString`.

## Product boundaries

The baseline intentionally has no business modules, public registration, or multitenancy. OIDC, MFA, email, jobs, storage, and tenancy are optional architecture choices. Add them only with product-specific security, lifecycle, operations, and testing requirements.
