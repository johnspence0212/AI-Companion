# EnterpriseStarter implemented plan

Status: implemented baseline. This document records what the repository provides and where product-specific work begins.

## Product goal

EnterpriseStarter is a secure, deployable foundation for a single application. It supplies administration and extension seams without inventing a business domain.

## Implemented scope

| Capability | Decision |
|------------|----------|
| Architecture | One modular-monolith deployment |
| Backend boundaries | Thin `apps/api` host, reusable `apps/platform`, `apps/module-abstractions` |
| API | Explicit `/api/v1` routes |
| Authentication | ASP.NET Identity password login and HttpOnly application cookie |
| Request integrity | Antiforgery token endpoint and `X-CSRF-TOKEN` validation on authenticated mutations |
| Authorization | Global Identity roles containing code-defined permission claims |
| User lifecycle | Admin creates users with temporary passwords; users must change them |
| Roles | Protected `Admin` and `Member`; authorized admins can manage custom roles |
| Audit | Persistent security events for login and administrative changes |
| Database | PostgreSQL only |
| Migrations | One EF Core migration stream owned by `apps/platform` |
| Modules | Explicit compile-time backend and frontend registration |
| Development | Aspire starts PostgreSQL, API, and Vite |
| Reference deployment | Compose starts PostgreSQL, one-shot migrator, API, and nginx web |
| PWA | Production web build is installable; same cookie SPA; `/api` is never cached |

`ModuleRegistry.Production` and `apps/web/src/modules/registry.ts` are intentionally empty. The starter has no business modules, public registration, or multitenancy.

## Security baseline

- Login is rate limited and account lockout is enabled.
- Browser credentials use the application cookie; bearer tokens are not stored by the SPA.
- The web client fetches a CSRF token and sends `X-CSRF-TOKEN` for mutations.
- Permission policies guard users, roles, and audit APIs.
- Security audit records include actor, subject, outcome, IP, and user agent where available.
- Security and correlation headers are applied to API responses.
- Production replicas share Data Protection keys.
- Production migrations run in a separate one-shot process.

## Extension decisions

These are not missing starter features; each needs product requirements and an explicit design:

- OIDC/SSO and identity-provider provisioning
- MFA and account recovery
- Email delivery and invitation links
- Background jobs and scheduling
- Object/file storage
- Multitenancy and data-isolation boundaries
- Product modules and their domain entities
- Offline data, web push, and background sync (the production SPA is already installable as a PWA)

If multitenancy is selected, define the tenant boundary, membership model, authorization semantics, data isolation, migration strategy, audit requirements, and tests before adding tenant columns or middleware.

## Acceptance criteria met

- The host references platform and module abstractions.
- Production starts with zero product modules.
- Identity users, global roles, permission claims, and security audit share PostgreSQL.
- All application APIs are rooted at `/api/v1`.
- Admin-created and bootstrap users are marked for password change.
- Aspire is the default developer path.
- Compose is documented as a reference deployment, not the only production architecture.
- Production web builds are installable PWAs of the same SPA; `/api` is never cached.

## Architecture decisions

See [ARCHITECTURE.md](./ARCHITECTURE.md) and the concise ADRs in [`adr/`](./adr/):

1. no mandatory tenancy
2. compile-time modules
3. global permission RBAC
4. cookie and CSRF authentication
5. PostgreSQL with a shared migration stream
6. explicit API versioning

See [PHASE-6-BACKLOG.md](./PHASE-6-BACKLOG.md) for optional product extensions.
