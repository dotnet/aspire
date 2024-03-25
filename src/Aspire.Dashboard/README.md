# .NET Aspire Dashboard

## Configuration

Configuration is obtained through `IConfiguration`, so it can be provided in several ways, such as via environment variables.

The dashboard has two kinds of endpoints: a browser endpoint for viewing the dashboard UI and an OTLP endpoint that hosts an OTLP service and receives telemetry.

- `ASPNETCORE_URLS` specifies one or more HTTP endpoints through which the dashboard frontend is served. Defaults to http://localhost:18888.
- `DOTNET_DASHBOARD_OTLP_ENDPOINT_URL` specifies the OTLP endpoint. Defaults to http://localhost:18889.

Endpoints are given names in Kestrel (`Browser` and `Otlp`) and can be configured using [Kestrel endpoint configuration](https://learn.microsoft.com/aspnet/core/fundamentals/servers/kestrel/endpoints#configure-endpoints-in-appsettingsjson).

For example, the default certificate used by HTTPS endpoints can be configured using the `ASPNETCORE_Kestrel__Certificates__Default__Path` and `ASPNETCORE_Kestrel__Certificates__Default__Password` environment variables. Alternatively, the certificate can be configured for individual endpoints, such as `ASPNETCORE_Kestrel__Endpoints__Browser__Path`, etc.

### Frontend

The dashboard's web application frontend supports OpenID Connect (OIDC). Set `Frontend:AuthMode` to `OpenIdConnect`, then add the following configuration:

- `Authentication:Schemes:OpenIdConnect:Authority` &mdash; URL to the identity provider (IdP)
- `Authentication:Schemes:OpenIdConnect:ClientId` &mdash; Identity of the relying party (RP)
- `Authentication:Schemes:OpenIdConnect:ClientSecret`&mdash; A secret that only the real RP would know
- Other properties of [`OpenIdConnectOptions`](https://learn.microsoft.com/dotnet/api/microsoft.aspnetcore.builder.openidconnectoptions) specified in configuration container `Authentication:Schemes:OpenIdConnect:*`

It may also be run unsecured. Set `Frontend:AuthMode` to `Unsecured`. This completely disables all security for the dashboard frontend. This setting is used during local development, but is not recommended if you attempt to host the dashboard in other settings.

### Resources

- `DOTNET_RESOURCE_SERVICE_ENDPOINT_URL` specifies the gRPC endpoint to which the dashboard connects for its data. There's no default. If this variable is unspecified, the dashboard shows OTEL data but no resource list or console logs.

The resource service client supports certificates. Set `ResourceServiceClient:AuthMode` to `Certificate`, then add the following configuration:

- `ResourceServiceClient:ClientCertificate:Source` (required) one of:
  - `File` to load the cert from a file path, configured with:
    - `ResourceServiceClient:ClientCertificate:FilePath` (required, string)
    - `ResourceServiceClient:ClientCertificate:Password` (optional, string)
  - `KeyStore` to load the cert from a key store, configured with:
    - `ResourceServiceClient:ClientCertificate:Subject` (required, string)
    - `ResourceServiceClient:ClientCertificate:KeyStore:Name` (optional, [`StoreName`](https://learn.microsoft.com/dotnet/api/system.security.cryptography.x509certificates.storename), defaults to `My`)
    - `ResourceServiceClient:ClientCertificate:KeyStore:Location` (optional, [`StoreLocation`](https://learn.microsoft.com/dotnet/api/system.security.cryptography.x509certificates.storelocation), defaults to `CurrentUser`)
- `ResourceServiceClient:Ssl` (optional, [`SslClientAuthenticationOptions`](https://learn.microsoft.com/dotnet/api/system.net.security.sslclientauthenticationoptions))

To opt-out of authentication, set `ResourceServiceClient:AuthMode` to `Unsecured`. This completely disables all security for the resource service client. This setting is used during local development, but is not recommended if you attempt to host the dashboard in other settings.

### OTLP

The OTLP endpoint can be secured with [client certificate](https://learn.microsoft.com/aspnet/core/security/authentication/certauth) or API key authentication.

- `Otlp:AuthMode` specifies the authentication mode on the OTLP endpoint. Possible values are `Certificate`, `ApiKey`, `Unsecured`. This configuration is required.
- `Otlp:ApiKey` specifies the API key for the OTLP endpoint when API key authentication is enabled. This configuration is required for API key authentication.

#### Telemetry Limits

Telemetry is stored in-memory. To avoid excessive memory usage, the dashboard has limits on the count and size of stored telemetry. When a count limit is reached, new telemetry is added, and the oldest telemetry is removed. When a size limit is reached, data is truncated to the limit.

- `DOTNET_DASHBOARD_OTEL_LOG_COUNT_LIMIT` specifies the maximum number of log entries. Defaults to 10,000.
- `DOTNET_DASHBOARD_OTEL_TRACE_COUNT_LIMIT` specifies the maximum number of traces. Defaults to 10,000.
- `DOTNET_DASHBOARD_OTEL_METRIC_COUNT_LIMIT` specifies the maximum number of metric data points. Defaults to 50,000.
- `DOTNET_DASHBOARD_OTEL_ATTRIBUTE_COUNT_LIMIT` specifies the maximum number of attributes on telemetry. Defaults to 128.
- `DOTNET_DASHBOARD_OTEL_ATTRIBUTE_LENGTH_LIMIT` specifies the maximum length of attributes. Defaults to unlimited.
- `DOTNET_DASHBOARD_OTEL_SPAN_EVENT_COUNT_LIMIT` specifies the maximum number of events on span attributes. Defaults to unlimited.

### Other

- `DOTNET_DASHBOARD_APPLICATION_NAME` specifies the application name to be displayed in the UI. This applies only when no resource service URL is specified. When a resource service exists, the service specifies the application name.
