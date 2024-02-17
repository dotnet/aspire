# .NET Aspire Dashboard

The dashboard is configured with the following keys:

- `DOTNET_DASHBOARD_OTLP_ENDPOINT_URL` specifies the OTLP endpoint, and defaults to http://localhost:18889.
- `ASPNETCORE_URLS` specifies the HTTP endpoint via which the dashboard web application is served, and defaults to http://localhost:18888.
- `DOTNET_RESOURCE_SERVICE_ENDPOINT_URL` specifies the gRPC endpoint to which the dashboard connects to for its data. There's no default. If this variable is unspecified, the dashboard shows OTEL data but no resource list or console logs.
- `DOTNET_DASHBOARD_APPLICATION_NAME` specifies the application name to be displayed in the UI. This only applies when no resource service URL is specified. When a resource service exists, the service specifies the application name.

Configuration is obtained through `IConfiguration` so can be provided in several ways, such as via environment variables.
