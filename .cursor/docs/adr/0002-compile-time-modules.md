# ADR 0002: Compile-time modules

- Status: Accepted

## Decision

Product modules implement `IEnterpriseModule` and are registered explicitly at compile time. The production backend and frontend registries are empty in the starter.

## Consequences

Dependencies, routes, permissions, and schema changes are reviewable and deterministic. Runtime discovery, per-customer enablement, and plugin loading are not supported; adding a module requires a build and deployment.
