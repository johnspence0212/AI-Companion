# ADR 0003: Global permission RBAC

- Status: Accepted

## Decision

Authorization uses global ASP.NET Identity roles containing stable, code-defined permission claims. Policies check permissions rather than role names.

## Consequences

`Admin` and `Member` are protected roles; custom roles can bundle registered permissions. Role assignments apply across the application. Products needing organization- or resource-scoped authorization must add a separate, explicit model.
