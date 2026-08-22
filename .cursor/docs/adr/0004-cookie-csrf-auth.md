# ADR 0004: Cookie and CSRF authentication

- Status: Accepted

## Decision

The browser authenticates with an HttpOnly ASP.NET Identity cookie. Authenticated mutations require an antiforgery request token in `X-CSRF-TOKEN`.

## Consequences

The SPA keeps no bearer token. The API and web should remain same-origin through the Vite or nginx proxy, and replicas must share Data Protection keys. OIDC may establish the same application cookie but does not replace the browser session protocol.
