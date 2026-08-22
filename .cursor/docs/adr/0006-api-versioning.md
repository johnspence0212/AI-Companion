# ADR 0006: Explicit API versioning

- Status: Accepted

## Decision

Application endpoints use the explicit `/api/v1` route prefix. The web client normalizes configured API bases to that prefix.

## Consequences

Breaking HTTP contract changes require a new route version and a deliberate compatibility plan. Health, OpenAPI UI, and operational endpoints are outside the application API prefix.
