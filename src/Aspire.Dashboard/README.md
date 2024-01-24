# .NET Aspire Dashboard

## As a dotnet tool

The dashboard can be packaged and executed as a `dotnet tool`.

Configuration is (currently) performed via environment variables:

- `DOTNET_DASHBOARD_OTLP_ENDPOINT_URL` specifies the OTLP endpoint, and defaults to http://localhost:18889.
- `ASPNETCORE_URLS` specifies the HTTP endpoint via which the dashboard web application is served, and defaults to http://localhost:18888.
- `DOTNET_RESOURCE_SERVICE_ENDPOINT_URL` specifies the gRPC endpoint to which the dashboard connects to for its data, and defaults to http://localhost:18999.
