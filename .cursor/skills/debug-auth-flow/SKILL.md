---
name: debug-auth-flow
description: Diagnoses EnterpriseStarter login, cookie, CSRF, permissions, and forced-password-change failures.
---

# Debug Auth Flow

## Quick checks

1. Web uses same-origin `/api/v1` (Vite proxy or nginx).
2. `fetch` includes `credentials: 'include'`.
3. API CORS allows the web origin with `AllowCredentials()`.
4. After login, verify the `enterprise_starter_auth` and antiforgery cookies.
5. Behind TLS terminator: `X-Forwarded-Proto` trusted; consider `Auth__CookieSecurePolicy=Always`.

## Login smoke

```bash
curl -i -c /tmp/cookies.txt -X POST http://localhost:5000/api/v1/auth/login \
  -H 'Content-Type: application/json' \
  -d '{"email":"admin@enterprisestarter.local","password":"AdminPassword123!"}'

curl -i -b /tmp/cookies.txt http://localhost:5000/api/v1/auth/me
curl -i -b /tmp/cookies.txt http://localhost:5000/api/v1/auth/csrf
```

Expect `Set-Cookie` on login and `200` from `/me`. For mutations, send the CSRF response token as `X-CSRF-TOKEN` with the same cookies.

## Common failures

| Symptom | Likely cause |
|---------|----------------|
| 401 on `/me` after login | Cross-origin without credentials / cookie blocked |
| Login 423 | Account lockout |
| Login 429 | Auth rate limit |
| Mutation 400 | Missing/stale CSRF token or cookie |
| No bootstrap admin | Migrator lacks `SEED_ADMIN_PASSWORD` |
| 403 on `/api/v1/users` | Missing global user permission |
| Forced password route | `mustChangePassword=true`; complete the change |
| No registration route | Expected; admins create users |

## Key files

- `apps/platform/Controllers.cs`
- `apps/platform/PlatformExtensions.cs`
- `apps/platform/Security.cs`
- `apps/web/src/stores/auth.ts`
- `apps/web/src/api/base/client.ts`
- `apps/web/vite.config.ts` (proxy `/api`)
