---
name: add-fullstack-entity
description: Adds a compile-time EnterpriseStarter product module or entity with shared EF migrations, permission policies, typed fetch, and Vue registration.
---

# Add a product module or entity

See [reference.md](reference.md). EnterpriseStarter has no sample business domain, so choose a product-owned module boundary first.

## Checklist

```text
- [ ] Define stable module permissions and domain model
- [ ] Implement IEnterpriseModule and any IEntityModelContributor
- [ ] Register the backend module in ModuleRegistry.Production
- [ ] Add a shared migration in apps/platform/Migrations
- [ ] Add explicit /api/v1 endpoints with permission policies
- [ ] Add typed web API calls through the shared client
- [ ] Add module routes/navigation and register in src/modules/registry.ts
- [ ] Test permissions, CSRF mutations, persistence, and UI behavior
```

## Rules

- Modules are compile-time dependencies. There is no runtime scanning, module table, or customer-specific enablement.
- Module permissions join the platform permission catalog and can be assigned to global roles.
- Module entities use `EnterpriseDbContext`; model contributors configure them.
- All schema changes join the platform's shared PostgreSQL migration stream.
- Admin/platform behavior stays in `apps/platform`; product behavior stays in a product-owned module project or folder.
- Web modules expose routes/navigation through the explicit registry and import UI from `@/ui`.

## Migration

```bash
dotnet ef migrations add AddFeature \
  --project apps/platform \
  --startup-project apps/api
```

## Verify

```bash
dotnet build EnterpriseStarter.sln
dotnet test apps/api.tests/Api.Tests.csproj
cd apps/web && npm run type-check && npm run lint && npm run test:unit -- --run
```

Exercise allowed and denied permissions, and verify every state-changing cookie request passes CSRF validation.

Multitenancy is a separate product architecture decision. Design and test its isolation model before introducing scoped entities.
