# Optional product extensions

EnterpriseStarter provides the security and administration baseline. The capabilities below are product choices, not promised starter features.

## Already in place (do not rewrite)

| Capability | Where |
|------------|-------|
| Identity user store | `ApplicationUser` + EF |
| App session | HttpOnly cookie (`enterprise_starter_auth`) + CSRF |
| Authorization | Global roles and `AppPermissions` |
| User provisioning | Admin-created users with temporary passwords |
| Same-origin SPA API | `/api/v1` through Vite or nginx |

**Rule:** an IdP may prove identity; application authorization remains permission-based unless the product deliberately chooses another model.

---

## SSO / OIDC (primary Phase 6 item)

### Target flow

```text
Browser → LoginView "Sign in with …"
       → GET /api/v1/auth/external/{provider}  (new endpoint)
       → IdP
       → /api/v1/auth/external/{provider}/callback
       → Link/create ApplicationUser
       → SignInManager cookie (same as password login)
       → Redirect to SPA; auth store hydrates /auth/me
```

### Implementation sketch

1. Add OIDC options (`Auth:External:*` or per-provider sections) — client id/secret from env/user secrets.
2. Register `AddAuthentication().AddOpenIdConnect(...)` (or Entra / Auth0 helpers) beside Identity cookies.
3. Add challenge + callback endpoints on `AuthController` (password login only today; no external auth routes yet).
4. On callback: `FindByLoginAsync` / create user / `AddLoginAsync`; then `SignInManager.SignInAsync`.
5. Decide whether IdP groups map to global roles; default to existing application assignments.
6. `LoginView`: full navigation to the challenge endpoint.
7. Keep `credentials: 'include'`; **do not** make the SPA an OIDC public client talking bearer tokens to the API as the default path.

Skill: `.cursor/skills/replace-auth/`.

### Providers (examples)

| Provider | Notes |
|----------|-------|
| Entra ID | Common enterprise default; multi-tenant vs single-tenant app registration |
| Auth0 / Okta | Standard OIDC; map `email` + `sub` |
| Keycloak | Self-hosted; good for offline/demo |

### Non-goals for first SSO slice

- Replacing RBAC with IdP claims alone
- Bearer tokens in SPA storage
- Organization-specific identity-provider selection without a defined organization model

---

## Other backlog items

| Item | Notes |
|------|-------|
| **MFA** | Identity 2FA or IdP-enforced MFA; prefer IdP when SSO is on |
| **Email** | Delivery provider, verification, recovery, and one-time invitation links |
| **Jobs** | Durable scheduler/queue, retries, idempotency, and operational visibility |
| **Storage** | Provider, authorization, retention, malware scanning, and lifecycle |
| **Audit expansion** | Add permission denials, exports, retention, and tamper controls as required |
| **Custom role editor UI** | Still code-defined permissions; UI only edits role→permission bundles |
| **IdP group → role mapping** | Explicit external group to global application role mapping |
| **Module packaging** | Keep compile-time registration unless deployment isolation is required |
| **Mobile / Bearer variant** | Separate client credentials path; cookie remains web default |
| **PWA offline / push** | Installability is already in the production SPA; offline data, push, and background sync need a product design |
| **Multitenancy** | Define tenant boundary, membership, RBAC scope, data isolation, migrations, and tests first |

---

## Definition of done for SSO (when built)

- Password and external login both end in the same cookie session
- Global permission RBAC remains authoritative unless deliberately redesigned
- Public registration remains absent unless explicitly added
- Deploy runbook updated with redirect URIs and secrets
- Integration test: external callback issues the app cookie and `/api/v1/auth/me` succeeds
