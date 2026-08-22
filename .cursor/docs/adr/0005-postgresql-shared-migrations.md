# ADR 0005: PostgreSQL and a shared migration stream

- Status: Accepted

## Decision

PostgreSQL is the only supported database provider. Platform and compile-time module schema changes use one `EnterpriseDbContext` and one ordered migration stream in `apps/platform/Migrations`.

## Consequences

Development, tests, and deployment exercise the same provider. A one-shot migrator applies production migrations before API replicas start. Modules cannot own independently deployed database migration histories inside this monolith.
