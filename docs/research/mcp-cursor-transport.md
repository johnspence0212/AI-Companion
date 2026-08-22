# .NET MCP hosting and Cursor transport constraints

**Researched:** 2026-08-22

## Question

What currently supported .NET MCP hosting and transport options can serve both local and
remote Cursor clients from a self-hosted application, and what constraints do they impose
on authentication, AI-client identity, streaming, current-repository discovery,
deployment, and testing?

The target is the existing .NET 10 modular monolith and Vue SPA. The browser already uses
an Identity cookie plus CSRF; data is private per user; MCP operations must identify both
the user and the authenticated AI-client class; and MCP adapters must invoke the same
application services as HTTP APIs.

## Executive answer

Publish one authenticated **Streamable HTTP** endpoint, such as `/mcp`, from the existing
ASP.NET Core application using the stable official C# MCP SDK 2.2.x. Both local
Cursor Desktop/CLI and Cursor Cloud can use a URL-based HTTP server. `stdio` remains a
supported local convenience adapter, but it cannot by itself serve remote clients.
Legacy HTTP+SSE is deprecated and is not supported for custom Cursor Cloud MCP servers
([Cursor MCP transports](https://cursor.com/docs/mcp),
[Cursor Cloud MCP](https://cursor.com/docs/cloud-agent/capabilities#mcp-tools),
[official C# SDK transports](https://csharp.sdk.modelcontextprotocol.io/v2/concepts/transports/transports.html)).

Keep the Vue cookie/CSRF flow for browsers. Protect `/mcp` as an OAuth bearer-token
resource with MCP Protected Resource Metadata. ASP.NET Core can host cookie and bearer
schemes together, while the official C# SDK integrates JWT bearer validation, MCP
authentication challenges, endpoint authorization, and primitive-level authorization
filters
([MCP authorization](https://modelcontextprotocol.io/specification/2026-07-28/basic/authorization),
[ASP.NET Core authentication](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/?view=aspnetcore-10.0),
[official protected MCP sample](https://github.com/modelcontextprotocol/csharp-sdk/blob/main/samples/ProtectedMcpServer/Program.cs)).

Treat the validated token subject as the user identity and a trusted
authorization-server client/credential claim as the AI-client identity. MCP
`clientInfo` is self-reported and explicitly unsuitable for security decisions
([MCP request metadata](https://modelcontextprotocol.io/specification/2026-07-28/basic)).
Standard MCP and Cursor OAuth do not authenticate an individual Cursor conversation,
model, or agent run; that requires a separate signed identity design.

Do not build V1 around protocol sessions, Roots, Sampling, implicit current-directory
state, or legacy SSE. MCP 2026-07-28 removed HTTP protocol sessions and deprecated Roots,
Sampling, Logging, and HTTP+SSE
([2026-07-28 changelog](https://modelcontextprotocol.io/specification/2026-07-28/changelog),
[deprecated feature registry](https://modelcontextprotocol.io/specification/2026-07-28/deprecated)).
Represent the current repository explicitly as a product repository ID or resource URI
and authorize it on every operation.

## Current stable baseline

The current protocol revision is MCP `2026-07-28`. The official C# SDK 2.0.0 aligned its
stable API with that revision, and stable 2.2.0 added hybrid stateful/stateless HTTP
serving on 2026-08-13
([C# SDK releases](https://github.com/modelcontextprotocol/csharp-sdk/releases),
[2.2.0 release](https://github.com/modelcontextprotocol/csharp-sdk/releases/tag/v2.2.0)).
The SDK runtime packages are stable; Microsoft's MCP project-template tooling remains
preview and is not required to host MCP
([official SDK versioning](https://csharp.sdk.modelcontextprotocol.io/v2/versioning.html),
[.NET MCP quickstart](https://learn.microsoft.com/en-us/dotnet/ai/quickstarts/build-mcp-server)).

Important 2026 differences from older examples:

- Current Streamable HTTP uses POST requests and has no standalone GET event stream,
  `Mcp-Session-Id`, or connection-scoped identity
  ([Streamable HTTP](https://modelcontextprotocol.io/specification/2026-07-28/basic/transports/streamable-http)).
- The legacy HTTP+SSE transport is deprecated
  ([Streamable HTTP compatibility](https://modelcontextprotocol.io/specification/2026-07-28/basic/transports/streamable-http#backward-compatibility)).
- Roots and Sampling remain functional during a deprecation window, but new
  implementations should use explicit parameters/resource URIs and direct model-provider
  integration instead
  ([deprecated features](https://modelcontextprotocol.io/specification/2026-07-28/deprecated)).
- OAuth Dynamic Client Registration is deprecated in favor of Client ID Metadata
  Documents, with pre-registration also supported. Cursor currently documents static
  pre-registration, not Client ID Metadata Documents
  ([MCP client registration](https://modelcontextprotocol.io/specification/2026-07-28/basic/authorization/client-registration),
  [Cursor static OAuth](https://cursor.com/docs/mcp#static-oauth-for-remote-servers)).

## Supported .NET hosting and transport options

### ASP.NET Core Streamable HTTP — recommended

`ModelContextProtocol.AspNetCore` supplies `WithHttpTransport()` and `MapMcp()` for an
MCP endpoint. The 2.x SDK defaults modern HTTP clients to stateless serving, which avoids
session affinity and permits horizontal scaling
([C# SDK HTTP transport](https://csharp.sdk.modelcontextprotocol.io/v2/concepts/transports/transports.html),
[stateless guidance](https://csharp.sdk.modelcontextprotocol.io/v2/concepts/stateless/stateless.html)).

Cursor describes Streamable HTTP as local/remote, server-deployed, and multi-user.
Cursor Cloud recommends HTTP; its backend proxies calls and does not expose configured
OAuth tokens or HTTP headers to the agent VM
([Cursor MCP](https://cursor.com/docs/mcp),
[Cursor Cloud HTTP vs stdio](https://cursor.com/docs/cloud-agent/capabilities#http-vs-stdio)).

This is the only one-server topology that satisfies local and remote V1 clients:

- Desktop/CLI and Cloud use the same HTTPS URL.
- The endpoint can live in the existing API deployment.
- MCP handlers remain delivery adapters over the same application command/query services
  used by controllers.
- Authentication, ownership, authorization, and durable state are reconstructed and
  checked per request rather than cached against a connection.

Cursor does not publish a protocol-version matrix for each surface. The compatibility-safe
starting configuration is therefore the SDK 2.2 hybrid
`StatefulForInitializeClients` mode: 2026-07-28 clients stay stateless, while older
`initialize` clients receive stateful compatibility on the same path
([HTTP session modes](https://csharp.sdk.modelcontextprotocol.io/v2/api/ModelContextProtocol.AspNetCore.HttpServerSessionMode.html)).
That legacy half needs affinity and bounded session lifecycle; after Desktop, CLI, and
Cloud tests prove current stateless negotiation, switch to fully stateless mode
([session deployment constraints](https://csharp.sdk.modelcontextprotocol.io/v2/concepts/stateless/stateless.html)).

### .NET stdio — supported, supplementary, local

The core SDK supports `WithStdioServerTransport()`. Cursor starts the configured command
as a local child process and classifies stdio as local, Cursor-managed, single-user, and
manually authenticated
([C# SDK stdio](https://csharp.sdk.modelcontextprotocol.io/v2/concepts/transports/transports.html),
[Cursor stdio configuration](https://cursor.com/docs/mcp#stdio-server-configuration)).

Stdio does not serve hosted clients from the self-hosted application. Cursor Cloud can
start stdio inside each cloud VM, but then the executable and dependencies must exist in
the VM and its environment secrets are visible to processes there. Cursor cannot verify
the server until a run starts and recommends HTTP where possible
([Cursor Cloud custom MCP](https://cursor.com/docs/cloud-agent/capabilities#custom-mcp-servers)).
The MCP OAuth flow is for HTTP; stdio should obtain credentials from its environment
([MCP authorization](https://modelcontextprotocol.io/specification/2026-07-28/basic/authorization)).

A future stdio bridge could provide direct local-filesystem behavior, but it adds
packaging, updates, sandboxing, secret bootstrap, and another hop to the hosted
application. It should proxy or reuse adapter/application code and must never contain a
second business-logic implementation.

### Legacy HTTP+SSE — compatibility only

Cursor Desktop still lists SSE, but Cursor Cloud says custom SSE and `mcp-remote` are not
supported. MCP deprecates HTTP+SSE; the C# SDK disables `/sse` and `/message` by default,
marks `EnableLegacySse` obsolete, and requires stateful mode to enable it
([Cursor transports](https://cursor.com/docs/mcp),
[Cursor Cloud custom MCP](https://cursor.com/docs/cloud-agent/capabilities#custom-mcp-servers),
[C# SDK SSE status](https://csharp.sdk.modelcontextprotocol.io/v2/concepts/transports/transports.html)).
Do not expose it unless measured interoperability evidence requires it.

### Stream/in-memory transport — tests and embedding

The SDK's `StreamServerTransport` and `StreamClientTransport` can connect through
in-memory pipes. This is supported for tests or same-process embedding, not as a Cursor
deployment endpoint
([C# SDK in-memory transport](https://csharp.sdk.modelcontextprotocol.io/v2/concepts/transports/transports.html#in-memory-transport)).

## Authentication and private ownership

An MCP HTTP server using OAuth is a protected resource. It advertises authorization
servers/scopes through OAuth Protected Resource Metadata and a `WWW-Authenticate`
challenge. Clients use authorization-server discovery, PKCE, and Resource Indicators;
the resource server must reject tokens not intended for its own audience/resource
([MCP authorization](https://modelcontextprotocol.io/specification/2026-07-28/basic/authorization),
[authorization security](https://modelcontextprotocol.io/specification/2026-07-28/basic/authorization/security-considerations)).

The official ASP.NET integration supports this composition:

1. Register the bearer validator, normally `AddJwtBearer`.
2. Add MCP challenge/resource metadata through the SDK's `AddMcp(...)`.
3. call `UseAuthentication()` and `UseAuthorization()`.
4. Protect `MapMcp()` with `RequireAuthorization()`.
5. Add `AddAuthorizationFilters()` for `[Authorize]` policies on tools, resources, and
   prompts
   ([protected-server sample](https://github.com/modelcontextprotocol/csharp-sdk/blob/main/samples/ProtectedMcpServer/Program.cs),
   [authorization filters](https://csharp.sdk.modelcontextprotocol.io/v2/concepts/filters.html)).

The Vue cookie remains appropriate for browser authentication, but it is not a complete
MCP OAuth contract. ASP.NET Core's built-in Identity bearer tokens are proprietary and
the official documentation says that facility is not intended as a full-featured
identity provider/token server
([Identity API authorization](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/identity-api-authorization?view=aspnetcore-10.0)).
V1 therefore needs a standards-compliant authorization server, either integrated/self-
hosted or external, while the application remains the bearer-token resource server.

Cursor's documented static OAuth setup uses a client ID, optional secret, and scopes.
Support both surfaces by registering both fixed callbacks:

- Desktop: `http://localhost:8787/callback`
- Web/Cursor Agents: `https://www.cursor.com/agents/mcp/oauth/callback`

([Cursor static OAuth](https://cursor.com/docs/mcp#static-oauth-for-remote-servers)).
Cursor Cloud says OAuth remains per-user even for team-shared MCP servers
([Cursor Cloud MCP](https://cursor.com/docs/cloud-agent/capabilities#mcp-tools)).

Cursor also supports configured HTTP headers, including bearer tokens. Those are useful
for development or managed service integrations, but a shared API key cannot meet
per-user ownership or client attribution, and long-lived personal tokens require explicit
rotation/revocation
([Cursor remote configuration](https://cursor.com/docs/mcp#using-mcpjson)).

OAuth only authenticates the caller. Every MCP adapter call must pass the validated actor
to shared application services, which must enforce record ownership and permissions on
every operation. The current protocol permits tool lists to vary by per-request
authorization but not by connection state
([MCP tools](https://modelcontextprotocol.io/specification/2026-07-28/server/tools)).

## Authenticated AI-client attribution

`io.modelcontextprotocol/clientInfo` is self-reported name/version metadata. The protocol
says it is for display, logs, and debugging, and should not alter behavior or security
decisions
([MCP `_meta`](https://modelcontextprotocol.io/specification/2026-07-28/basic#_meta)).
Do not treat `clientInfo`, User-Agent, IP address, workspace path, process identity, or a
legacy session ID as authenticated client identity.

For every audited operation, persist:

1. the stable product user mapped from the validated access token's `sub`;
2. a trusted OAuth client/credential identifier exposed by token claims or introspection;
3. the tool/resource name, affected product entities, idempotency key, and trace ID; and
4. MCP `clientInfo` only as explicitly untrusted diagnostics.

MCP client registration establishes a client ID but does not mandate one particular
access-token claim. The chosen authorization server must deliberately expose that trusted
client identity to the resource server. If Desktop and Cloud share one static OAuth
client, the server can attribute “Cursor” but cannot distinguish those surfaces or an
installation; separate registrations or per-installation credentials are required when
that distinction matters
([MCP client registration](https://modelcontextprotocol.io/specification/2026-07-28/basic/authorization/client-registration),
[ASP.NET Core claims](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/claims?view=aspnetcore-10.0)).

Cursor Cloud can mint signed, audience-bound OIDC identity tokens inside managed VMs, but
agent metadata itself is not a credential, self-hosted workers do not expose that metadata
API, and Cursor does not document automatically forwarding a run identity to arbitrary
remote MCP servers
([Cursor Cloud identity](https://cursor.com/docs/cloud-agent/identity),
[Cursor agent metadata](https://cursor.com/docs/cloud-agent/metadata)).
Per-conversation or per-run proof is therefore a separate Cursor-specific product design,
not a property of standard MCP OAuth.

## Streaming and long-running operations

Current Streamable HTTP supports a normal JSON result or an SSE response to the same POST.
The SSE response can carry notifications related to that request before its final result.
Progress is optional and only applies when the client supplied a unique `progressToken`
([Streamable HTTP](https://modelcontextprotocol.io/specification/2026-07-28/basic/transports/streamable-http),
[MCP progress](https://modelcontextprotocol.io/specification/2026-07-28/basic/patterns/progress)).
Cursor does not promise progress presentation in its public feature list, so progress
must be advisory rather than required for correctness
([Cursor protocol support](https://cursor.com/docs/mcp#protocol-and-extension-support)).

Long-lived change notifications use a `subscriptions/listen` POST-response stream in the
current protocol
([MCP subscriptions](https://modelcontextprotocol.io/specification/2026-07-28/basic/patterns/subscriptions)).
Cursor's public MCP page does not explicitly promise that method; require a compatibility
test before making it a V1 contract.

The 2026 revision removed SSE resumability/redelivery. If a response stream breaks, a
client reissues the operation as a new request
([2026-07-28 changelog](https://modelcontextprotocol.io/specification/2026-07-28/changelog)).
Mutating tools need application-level idempotency keys or retry-safe semantics. Operations
that must survive disconnects should create durable PostgreSQL jobs/entities and expose
status tools; progress is presentation, not durable state.

Modern server-to-client input uses Multi Round-Trip Requests (MRTR): the server returns an
input-required result and the client retries with responses
([MCP MRTR](https://modelcontextprotocol.io/specification/2026-07-28/basic/patterns/mrtr)).
Cursor documents Elicitation support, but V1 should use it only for optional input with an
explicit-parameter fallback. Do not depend on Sampling; it is deprecated and Cursor does
not list it among supported capabilities
([Cursor protocol support](https://cursor.com/docs/mcp#protocol-and-extension-support),
[deprecated features](https://modelcontextprotocol.io/specification/2026-07-28/deprecated)).

## Current-repository discovery

There is no cross-surface, authenticated “current repository” in a standard remote MCP
request.

Cursor can interpolate `${workspaceFolder}` into local `mcp.json` fields. That is local
configuration text, not a durable repository identity, and it grants a remote application
no filesystem access
([Cursor interpolation](https://cursor.com/docs/mcp#config-interpolation)).

Cursor supports MCP Roots, but Roots are informational `file://` URIs rather than access
control, may contain multiple entries, and are now deprecated. The current specification
directs new implementations toward tool parameters, resource URIs, or server
configuration
([MCP Roots](https://modelcontextprotocol.io/specification/2026-07-28/client/roots),
[deprecated Roots](https://modelcontextprotocol.io/specification/2026-07-28/deprecated)).
Cursor Cloud has repository metadata inside its VM, but does not document forwarding it
to arbitrary remote MCP servers
([Cursor agent metadata](https://cursor.com/docs/cloud-agent/metadata)).

Model repository context in the product:

- assign each repository/workspace an immutable product ID;
- pass that ID explicitly in relevant tools/resource URIs, or use a short-lived
  server-minted handle;
- authorize the user against the repository on every call;
- optionally retain a per-user default; and
- if a future local adapter maps checkout metadata to a product ID, validate the mapping
  server-side and never treat a path/remote URL as authorization.

Current MCP guidance likewise requires cross-call state to use explicit handles and says
authenticated servers must re-authorize each handle on every use
([stateful tool guidance](https://modelcontextprotocol.io/specification/2026-07-28/server/tools#stateful-tools)).

## Deployment constraints

- Expose `/mcp` over HTTPS for Cursor Cloud; a LAN-only or localhost endpoint serves only
  local clients.
- Publish Protected Resource Metadata and return MCP-compatible bearer challenges.
- Validate token signature, issuer, expiry, audience/resource, scopes, user, and trusted
  client identity on every request
  ([ASP.NET Core JWT validation](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/configure-jwt-bearer-authentication?view=aspnetcore-10.0)).
- Validate `Origin` when present, as required by Streamable HTTP, and use an explicit
  allowlist rather than permissive CORS
  ([Streamable HTTP security](https://modelcontextprotocol.io/specification/2026-07-28/basic/transports/streamable-http#security-warning)).
- Prefer fully stateless serving after compatibility verification. While hybrid mode is
  enabled, route legacy sessions with affinity and bound their lifetime/memory.
- Configure ingress for unbuffered SSE responses, bounded request/idle timeouts,
  cancellation, request-size limits, and rate limits by authenticated user and client.
- Persist ownership, audit events, jobs, and idempotency records in PostgreSQL, not
  transport-session memory.
- Keep MCP handlers thin. They call the same application services and transactions as
  HTTP; MCP must not become a second authorization or domain layer.

## Testing strategy

1. Test shared application services for private ownership, permissions, idempotency, and
   audit attribution independently of transport.
2. Use official in-memory stream transports for protocol list/read/call tests without a
   process or network
   ([C# in-memory transport](https://csharp.sdk.modelcontextprotocol.io/v2/concepts/transports/transports.html#in-memory-transport)).
3. Use `WebApplicationFactory`/`TestServer` for `/mcp`, authentication challenges,
   Protected Resource Metadata, bad issuer/audience/scope, cross-user denial, trusted
   client attribution, JSON/SSE responses, cancellation, concurrency, and retries
   ([ASP.NET Core integration tests](https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-10.0)).
4. While hybrid mode is enabled, test both a stateless 2026-07-28 request and an older
   initialize/session flow.
5. Run actual Cursor Desktop, CLI, and Cloud smoke tests for both OAuth callbacks,
   tool/resource discovery, approval, mutation, progress visibility, disconnect/retry,
   and explicit repository context. Cursor CLI uses the editor's MCP configuration and
   supports `agent mcp login`
   ([Cursor CLI MCP](https://cursor.com/docs/cli/mcp)).
6. Test through the production proxy and multiple replicas to verify SSE behavior and
   session routing during the compatibility window.
7. If stdio is later shipped, reserve child-process tests for startup, stdout framing,
   cancellation, credential bootstrap, and proxy failure. Use in-memory transports for
   most behavior; the SDK warns against using real stdio in unit tests because stdin may
   leave a blocked reader
   ([official SDK test guidance](https://github.com/modelcontextprotocol/csharp-sdk/blob/main/CONTRIBUTING.md#choosing-a-transport)).

## Recommended V1 topology

1. Add a thin MCP adapter to the existing ASP.NET Core deployment and map one `/mcp`
   Streamable HTTP endpoint using stable `ModelContextProtocol.AspNetCore` 2.2.x.
2. Initially use `StatefulForInitializeClients` because Cursor publishes no per-surface
   protocol matrix. Keep legacy `/sse` disabled. Move to fully stateless mode when
   Desktop, CLI, and Cloud compatibility tests pass.
3. Keep Identity cookie + CSRF for Vue. Add a standards-compliant OAuth authorization
   server/integration and a dedicated bearer policy plus MCP Protected Resource Metadata
   for `/mcp`.
4. Pre-register Cursor for V1 using its documented static OAuth flow and both callback
   URLs. Issue per-user, resource-bound, least-privilege tokens.
5. Audit the trusted user subject and trusted OAuth client/credential ID on every
   operation. Store `clientInfo` only as untrusted diagnostics.
6. Invoke the same application services as HTTP and enforce ownership/permissions there.
7. Pass product repository IDs/resources explicitly; do not depend on Roots or an
   implicit current directory.
8. Use idempotency keys and durable PostgreSQL job IDs for mutation/long work. Treat
   progress and elicitation as optional UX.
9. Ship no stdio adapter initially. Add one only for a proven local-filesystem use case.

## Facts that remain product decisions

- **Authorization server:** self-host a standards-compliant OAuth/OIDC server or integrate
  an external issuer; define consent, scopes, audience, refresh, revocation, and account
  linking.
- **AI-client granularity:** identify “Cursor,” distinguish Desktop from Cloud, identify
  installations, or prove individual agent runs. Each stronger level needs separate
  registrations/credentials or a Cursor-specific signed assertion.
- **Client registration:** use conservative static pre-registration for V1 or later adopt
  Client ID Metadata Documents after Cursor documents support. Dynamic registration is
  deprecated.
- **Compatibility window:** supported Cursor versions/surfaces, how long hybrid sessions
  remain, and whether telemetry ever justifies legacy SSE.
- **Repository UX:** explicit repository per call, resource URI, re-authorized context
  handle, per-user default, or a future local checkout mapper.
- **Interactive behavior:** which operations may use Elicitation/MRTR, fallback behavior,
  and whether progress is displayed.
- **Long-running contract:** product job polling only or an MCP extension after explicit
  Cursor compatibility testing.
- **Network model:** internet-public endpoint, customer-controlled gateway/tunnel, or
  both. Cursor Cloud cannot call a LAN-only host.
- **Token/audit lifecycle:** token duration, rotation, revocation, actor/client fields,
  redaction, retention, and privacy treatment of AI-origin metadata.

## Primary sources

- Cursor, [Model Context Protocol](https://cursor.com/docs/mcp)
- Cursor, [Cloud Agent MCP capabilities](https://cursor.com/docs/cloud-agent/capabilities#mcp-tools)
- Cursor, [CLI MCP](https://cursor.com/docs/cli/mcp)
- Cursor, [Cloud Agent identity](https://cursor.com/docs/cloud-agent/identity)
- Cursor, [Cloud Agent metadata](https://cursor.com/docs/cloud-agent/metadata)
- MCP, [2026-07-28 specification](https://modelcontextprotocol.io/specification/2026-07-28)
- MCP, [Streamable HTTP](https://modelcontextprotocol.io/specification/2026-07-28/basic/transports/streamable-http)
- MCP, [Authorization](https://modelcontextprotocol.io/specification/2026-07-28/basic/authorization)
- MCP, [Deprecated features](https://modelcontextprotocol.io/specification/2026-07-28/deprecated)
- Official C# SDK, [documentation](https://csharp.sdk.modelcontextprotocol.io/v2/)
- Official C# SDK, [2.2.0 release](https://github.com/modelcontextprotocol/csharp-sdk/releases/tag/v2.2.0)
- Microsoft, [ASP.NET Core authentication](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/?view=aspnetcore-10.0)
- Microsoft, [ASP.NET Core JWT bearer authentication](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/configure-jwt-bearer-authentication?view=aspnetcore-10.0)

