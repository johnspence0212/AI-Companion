---
name: replace-auth
description: Adds or swaps an external IdP (Entra ID, Auth0, Keycloak) onto the Identity cookie baseline. Use for SSO/OIDC — keep issuing the application cookie after external sign-in.
---

# Replace / Extend Auth Provider

Baseline is ASP.NET Identity with an HttpOnly application cookie and CSRF validation. OIDC is an optional product extension; it should link external identities to `ApplicationUser` and establish the same app cookie.

Design notes / acceptance criteria: [.cursor/docs/PHASE-6-BACKLOG.md](../../docs/PHASE-6-BACKLOG.md).  
Touch points: [reference.md](reference.md).

## Planning

1. Choose OIDC provider
2. Keep cookie session as the API credential for the SPA
3. Define external subject linking and optional mapping to global roles

## Backend (`apps/api`)

| Area | Action |
|------|--------|
| `apps/platform/PlatformExtensions.cs` | Add external authentication/OIDC |
| Platform controller or endpoints | Add versioned challenge + callback |
| Cookie config | Unchanged for SPA API calls |
| Roles | Deliberately map IdP groups to global roles, or preserve application assignments |

## Frontend (`apps/web`)

| Area | Action |
|------|--------|
| `LoginView.vue` | Add “Sign in with …” that hits external challenge |
| `stores/auth.ts` | Still hydrate via `/api/v1/auth/me` after redirect |
| `httpClient` | Keep `credentials: 'include'` |

## Constraints

- Do not move the SPA back to bearer tokens in `sessionStorage` as the default
- Keep APIs behind `[Authorize]` and permission policies
- Preserve CSRF validation after the external sign-in
- Update CORS / redirect URIs for the IdP
- Do not commit client secrets
