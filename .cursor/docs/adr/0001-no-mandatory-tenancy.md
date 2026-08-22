# ADR 0001: No mandatory tenancy

- Status: Accepted

## Decision

EnterpriseStarter is single-application by default. It contains no tenant entity, membership, resolver, tenant cookie/header, or tenant query filter.

## Consequences

The baseline stays usable for single-organization and non-tenant products. A product choosing multitenancy must explicitly design identity membership, authorization scope, data isolation, migrations, operations, audit, and isolation tests.
