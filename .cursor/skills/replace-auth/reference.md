# Auth Touch Points (cookie Identity baseline)

## Backend

| File | Role |
|------|------|
| `apps/platform/PlatformExtensions.cs` | Identity, cookie, CSRF, rate limits, Data Protection |
| `apps/platform/Controllers.cs` | Versioned auth and administration endpoints |
| `apps/platform/Domain.cs` | `ApplicationUser`, global roles/permissions, auth options |
| `apps/platform/Security.cs` | Permission claims and security audit |
| `apps/platform/Data.cs` | Identity persistence |

## Frontend

| File | Role |
|------|------|
| `apps/web/src/stores/auth.ts` | Session, global roles/permissions, password-change state |
| `apps/web/src/api/authApi.ts` | Auth HTTP calls |
| `apps/web/src/api/base/client.ts` | `credentials: 'include'` |
| `apps/web/src/router/index.ts` | Auth, password-change, permission, module routes |
| `apps/web/src/views/LoginView.vue` | Login UI |
| `apps/web/src/main.ts` | `auth.hydrate()` |

## Current cookie flow

1. `POST /api/v1/auth/login` sets the HttpOnly app cookie and returns the user.
2. Browser sends cookies through the shared client.
3. Client gets `/api/v1/auth/csrf` and adds `X-CSRF-TOKEN` to mutations.
4. `GET /api/v1/auth/me` hydrates the SPA.
5. `POST /api/v1/auth/logout` validates CSRF and clears the session.

## Phase 6 SSO

Wire OIDC into Identity and keep issuing the same application cookie after external sign-in. EnterpriseStarter does not currently include external challenge/callback endpoints.
Design: `.cursor/docs/PHASE-6-BACKLOG.md`.
