---
name: run-ci-locally
description: Reproduces GitHub Actions CI locally — NuGet/npm vuln gates, dotnet test, web type-check/lint/unit/build, and Playwright e2e. Use before pushing, when validating PRs, or matching CI failures.
disable-model-invocation: false
---

# Run CI Locally

Mirrors `.github/workflows/ci.yml`. **Postgres must be running** for api and e2e jobs.

## Job: api

```bash
# PostgreSQL DB: enterprise_starter_tests
dotnet restore EnterpriseStarter.sln
dotnet list EnterpriseStarter.sln package --vulnerable --include-transitive
dotnet build EnterpriseStarter.sln --no-restore
Database__ConnectionString='Host=localhost;Port=5432;Database=enterprise_starter_tests;Username=enterprise_starter;Password=enterprise_starter' \
  dotnet test apps/api.tests/Api.Tests.csproj --verbosity normal
```

## Job: web

```bash
cd apps/web
npm ci
npm audit --omit=dev --audit-level=high
npm run type-check
npm run lint
npm run test:unit -- --run
npm run build
npm run build-storybook
npm run api:check
```

## Job: e2e

Requires API/web tooling and a PostgreSQL E2E database:

```bash
cd e2e
npm ci
npx playwright install chromium --with-deps
CI=true Database__ConnectionString='Host=localhost;Port=5432;Database=enterprisestarter_e2e;Username=enterprise_starter;Password=enterprise_starter' npm test
```

Playwright config starts api and web automatically (`e2e/playwright.config.ts`).

## Prerequisites

- .NET 10 SDK
- Node 22+
- PostgreSQL 16+ with test databases
- Chromium deps for Playwright (install step above)

On failure, use skill `fix-ci-failure`.
