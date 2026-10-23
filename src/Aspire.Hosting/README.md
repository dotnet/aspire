# Aspire.Hosting library

Core abstractions for the Aspire application model. It provides the building blocks for the distributed application
hosting model. This package should not be referenced by AppHost projects directly. Instead use the `Aspire.Hosting.AppHost`
package to add a transitive referencing including custom build targets to support code generation of metadata
types for referenced .NET projects.

Developers wishing to build their own custom resource types and supporting APIs for Aspire should reference
this package directly.

## Aspire Application Model Overview

Aspire models distributed applications as a graph of **resources**—services, infrastructure elements, and supporting components—using strongly-typed, extensible abstractions. Resources are inert data objects that describe capabilities, configuration, and relationships. Developers compose applications using fluent extension methods (like `AddProject`, `AddPostgres`, etc.), wire dependencies explicitly, and attach metadata through annotations to drive orchestration, configuration, and deployment.

Key concepts include:

- **Resources:** The fundamental unit representing a service or component in the app model.
- **Annotations:** Extensible metadata attached to resources to express capabilities and configuration.
- **Fluent extension methods:** APIs like `AddX`, `WithReference`, and `WithEnvironment` that guide correct resource composition and wiring.
- **Resource graph:** An explicit, developer-authored Directed Acyclic Graph (DAG) that models dependencies and value flows.
- **Deferral and structured values:** Configuration and connectivity are expressed using structured references, allowing for deferred evaluation and environment-specific resolution at publish and run time.
- **Standard interfaces:** Optional interfaces enable polymorphic behaviors, such as environment wiring and endpoint exposure, for both built-in and custom resources.
- **Lifecycle events and resource states:** The Aspire runtime orchestrates resource startup, readiness, health, and shutdown in a predictable, observable way.

Aspire's approach ensures flexibility, strong tooling support, and clear separation between modeling, orchestration, and execution of distributed .NET applications.

For the full details and specification, see the [App Model document](https://github.com/microsoft/aspire/blob/main/docs/specs/appmodel.md).

## Browser URL references

Use `GetBrowserOrigin()` as a scalar reference to the preferred browser origin, without changing host or container service discovery:

```csharp
var browserUrls = frontend.GetBrowserOrigin("https");
backend.WithEnvironment("FRONTEND_ORIGIN", browserUrls);
backend.WithEnvironment("ALLOWED_ORIGINS", browserUrls.Origins.Join(";"));
backend.WithEnvironment("ALLOWED_ORIGINS_JSON", browserUrls.Origins.AsJson());
backend.WithEnvironment("CALLBACK_URL", ReferenceExpression.Create($"{browserUrls}/signin-oidc"));
```

Direct use and interpolation select one origin (scheme, host, and port), excluding navigation paths, queries, and
fragments. Use `.Origins` explicitly when the consumer needs all origins, or `.PreferredUrl` for the full launch URL.
These selections and formatting operations configure the same `BrowserOriginReference` value-provider type;
they do not introduce additional provider types. Use ordinary `ReferenceExpression` interpolation to compose
the selected value with paths or other configuration.

Values resolve after all `WithUrls` and `WithUrlForEndpoint` callbacks complete, before
`ResourceEndpointsAllocatedEvent` handlers run. Preferred values follow dashboard display ordering.
Collections include final HTTP(S) URLs associated with the resource's own endpoints, including detail-only aliases;
URLs without endpoint associations and links to another resource's endpoints are excluded. Origins are normalized and deduplicated,
while full URL projections preserve paths, queries, and fragments. A resource with no remaining browser URLs has
an empty collection; requesting its preferred URL or origin reports an error.

For advanced consumers, `GetUrlsAsync()` returns URL and endpoint metadata, and `CreateValue` provides a deferred
consumer callback. These callbacks consume finalized URLs; they do not run inside URL customization callbacks.
An integration can use them to expand multiple browser origins and callback paths into redirect configuration.

Publishing does not assume that a local browser address is a deployed public address. Supply an expression
supported by the integration or publisher using `WithPublishExpression`:

```csharp
var publicOrigins = builder.AddParameter("public-origins");
var allowedOrigins = browserUrls.Origins.AsJson()
    .WithPublishExpression(ReferenceExpression.Create($"{publicOrigins}"))
    .ToReferenceExpression(builder.ExecutionContext);
backend.WithEnvironment("ALLOWED_ORIGINS_JSON", allowedOrigins);
```

`ToReferenceExpression` returns a runtime expression containing the deferred browser provider in run mode, or
the configured ordinary deployment expression in publish/deploy mode. Call it when configuring the consuming value,
after the resource/environment configuration needed to construct the deployment expression. Native publishers see
only standard reference expressions, parameters, and outputs; they do not expand browser-specific provider types.

The publishing expression must produce the same configuration shape as the runtime projection (a JSON array in
this example). Existing publisher-specific callbacks can construct native parameter, output, or endpoint expressions.
A projection without explicit publishing support fails rather than substituting a local address.
For azd manifest publishing, browser references are represented by generated `value.v0` resources and standard
`{resource.connectionString}` expressions. Their values retain the configured deployment parameter/output
expressions; no browser-specific binding syntax or local URL evaluation is required.
Conditional manifest expressions evaluate their conditions during publishing and retain the selected branch's
supported parameter/output expressions for deployment-time resolution. Only the selected path is serialized,
including nested conditionals and browser references; inactive branches do not require browser publishing expressions.
Conditions must be resolvable during publishing. This does not add deploy-time conditional evaluation to azd
or change concrete value resolution in other publishers.

Manifest publication preserves references in the existing manifest vocabulary, such as endpoint URLs, parameters,
connection strings, and infrastructure outputs. Values whose references cannot be represented in that vocabulary
are resolved during publication and emitted as generated `value.v0` resources. For example, an endpoint's TLS-enabled
property becomes a concrete boolean string, while a parameter next to it remains a deployment-time reference.
Providers wrapping other expressions can expose those objects through `IValueWithReferences` so their emitted
references receive the same processing. Inactive conditional branches are not materialized. A required concrete
value that cannot resolve during publication reports an error rather than substituting a placeholder.
Concrete fallback values are shared only when the same provider instance is reused, not merely when two providers
advertise the same expression text. Generated names use deterministic collision suffixes.
Browser references likewise keep a stable alias per instance, including when their publishing expressions
have identical text but different formatting or provider instances. Reusing a reference reuses its alias;
separately constructed references remain distinct. Suffixes are assigned in first-reference order within the resource.

URI-component formatting encodes literal segments and publication-time values locally, while leaving parameters
and infrastructure outputs deferred through the manifest's existing URI filter. Formatting a generated browser or
conditional alias uses its already-selected expression; it does not evaluate inactive branches. Opaque symbolic
compositions that cannot preserve URI formatting report an unsupported-formatting error instead of partially
encoding the value or forcing deployment outputs to resolve during publication.
Publisher-generated formatting aliases retain their source expression and format, so nested URI formatting
preserves every encoding pass without resolving deployment parameters. Wrappers exposing a complete
`ReferenceExpression` preserve its ordered provider occurrences and formats. If an opaque wrapper exposes
multiple distinct providers for the same token without enough expression structure to distinguish them,
publication reports an ambiguity error instead of silently choosing a provider.

## Feedback & contributing

https://github.com/microsoft/aspire
