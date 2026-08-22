# Product module reference

## Backend

| Concern | Path |
|---------|------|
| Module contract and production registry | `apps/module-abstractions/IEnterpriseModule.cs` |
| Host composition | `apps/api/Program.cs` |
| Platform wiring | `apps/platform/PlatformExtensions.cs` |
| Model contribution contract | `apps/platform/Data.cs` |
| Permission catalog | `apps/platform/Domain.cs` |
| Shared DbContext and factory | `apps/platform/Data.cs` |
| Shared migrations | `apps/platform/Migrations/` |

An `IEnterpriseModule` contributes services, endpoints, and permissions. Register an instance in `ModuleRegistry.Production`. A module with entities registers an `IEntityModelContributor` from `AddServices`.

## Web

| Concern | Path |
|---------|------|
| Compile-time module registry | `apps/web/src/modules/registry.ts` |
| HTTP and CSRF client | `apps/web/src/api/base/client.ts` |
| Shared types | `apps/web/src/api/types/schema.ts` |
| Router aggregation | `apps/web/src/router/index.ts` |
| Navigation aggregation | `apps/web/src/components/AppSidebar.vue` |
| UI barrel | `apps/web/src/ui/index.ts` |
| Page/form components | `apps/web/src/components/` |

Each web module is explicitly registered and may expose routes and navigation guarded by a global permission.

## API conventions

- Explicit `/api/v1/{resource}` route
- `[Authorize]` and permission policy
- `ProblemDetails` errors
- Paged collections where appropriate
- Automatic CSRF validation for authenticated non-safe methods
