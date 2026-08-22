---
name: run-dev-environment
description: Starts the Vue + .NET full stack via Aspire AppHost or standalone api/web processes. Use when the user wants to run, start, or debug the dev environment, local servers, dashboard, or hot reload.
disable-model-invocation: false
---

# Run Dev Environment

**Not Docker.** For containerized deployment, use skill `run-docker-deploy`. Docker has no Vite HMR and is not the default workflow.

## Preferred: Aspire AppHost

```bash
dotnet run --project aspire/AppHost
```

- Opens Aspire dashboard with **postgres**, **api**, and **web**
- Injects the same-origin API proxy; web normalizes to `/api/v1`
- API migrates/seeds on startup in Development
- Health: `/health` (dev), `/health/ready` (DB)

Use the **proxied web** URL from the dashboard (not a cross-origin API base).

Default Aspire / `npm run dev` does **not** register a PWA service worker. To test install locally (same Vite proxy):

```bash
cd apps/web
VITE_API_BASE_URL=/api/v1 VITE_API_PROXY_TARGET=http://localhost:5000 npm run dev:pwa
```

Chromium should offer **Install** in the address bar. Details: `apps/web/README.md`.

Optional UI catalogue (does not start the SPA):

```bash
cd apps/web
npm run storybook
# http://localhost:6006
```

## Standalone (without Aspire)

Requires a reachable Postgres.

**Terminal 1 — API:**
```bash
dotnet run --project apps/api
# http://localhost:5000 — Scalar at /scalar (dev)
```

**Terminal 2 — Web:**
```bash
cd apps/web
cp .env.example .env   # if missing
VITE_API_BASE_URL=/api/v1 VITE_API_PROXY_TARGET=http://localhost:5000 npm run dev
# http://localhost:5173
```

## First-time setup

```bash
dotnet restore EnterpriseStarter.sln
cd apps/web && npm ci
```

Development bootstrap admin: `admin@enterprisestarter.local` / `AdminPassword123!`. The first session must change this password.

## Verify

1. Open web → login with seed admin
2. Change the temporary password; verify Users, Roles, and Security audit for Admin
3. Cookie smoke:

```bash
curl -c /tmp/c.txt -X POST http://localhost:5000/api/v1/auth/login \
  -H 'Content-Type: application/json' \
  -d '{"email":"admin@enterprisestarter.local","password":"AdminPassword123!"}'
curl -b /tmp/c.txt http://localhost:5000/api/v1/auth/me
curl -b /tmp/c.txt http://localhost:5000/api/v1/auth/csrf
```

## Troubleshooting

| Issue | Fix |
|-------|-----|
| CORS / cookie not set | Use same-origin `/api` proxy; `credentials: 'include'` |
| 401 after login | Cross-origin API URL or missing credentials |
| 400 on authenticated mutation | Fetch and send the CSRF request token |
| 403 on `/api/v1/users` | Need the corresponding global permission |
| Port in use | Change API `--urls` or Vite `--port` |
| Stale schema | Drop/recreate Postgres DB or add migration; restart API |
