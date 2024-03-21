# .NET Aspire Dashboard

## Configuration

The dashboard is configured with the following keys:

- `DOTNET_DASHBOARD_OTLP_ENDPOINT_URL` specifies the OTLP endpoint, and defaults to http://localhost:18889.
- `ASPNETCORE_URLS` specifies the HTTP endpoint via which the dashboard web application is served, and defaults to http://localhost:18888.
- `DOTNET_RESOURCE_SERVICE_ENDPOINT_URL` specifies the gRPC endpoint to which the dashboard connects to for its data. There's no default. If this variable is unspecified, the dashboard shows OTEL data but no resource list or console logs.
- `DOTNET_DASHBOARD_APPLICATION_NAME` specifies the application name to be displayed in the UI. This only applies when no resource service URL is specified. When a resource service exists, the service specifies the application name.

Configuration is obtained through `IConfiguration` so can be provided in several ways, such as via environment variables.

## Auth

The dashboard also supports authentication for its various network endpoints.

To opt-out of authentication, set the following environment variables to `1`:

- `DOTNET_RESOURCE_SERVICE_DISABLE_AUTH`
- `DOTNET_DASHBOARD_WEB_DISABLE_AUTH`
- `DOTNET_DASHBOARD_OTLP_DISABLE_AUTH`

These variables are set when running the dashboard during local development.

To configure auth, the above variables must be undefined or `0`. Additional configuration is required per connection.

### Resource Service

The resource service can be configured to require certificates with the following configuration values:

- `ResourceServiceClient:ClientCertificate:Source` which has one of the following values:
  - `File` to load the cert from a file path, configured with:
    - `ResourceServiceClient:ClientCertificate:FilePath` (required, string)
    - `ResourceServiceClient:ClientCertificate:Password` (optional, string)
  - `KeyStore` to load the cert from a key store, configured with:
    - `ResourceServiceClient:ClientCertificate:Subject` (required, string)
    - `ResourceServiceClient:ClientCertificate:KeyStore:Name` (optional, [`StoreName`](https://learn.microsoft.com/dotnet/api/system.security.cryptography.x509certificates.storename), defaults to `My`)
    - `ResourceServiceClient:ClientCertificate:KeyStore:Location` (optional, [`StoreLocation`](https://learn.microsoft.com/dotnet/api/system.security.cryptography.x509certificates.storelocation), defaults to `CurrentUser`)

Additional `SslClientAuthenticationOptions` may be configured via configuration in the `ResourceServiceClient:Ssl` key.
