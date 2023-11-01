var builder = DistributedApplication.CreateBuilder(args);

var grafana = builder.AddContainer("grafana", "grafana/grafana")
                     .WithVolumeMount("../grafana/config", "/etc/grafana")
                     .WithVolumeMount("../grafana/dashboards", "/var/lib/grafana/dashboards")
                     .WithServiceBinding(containerPort: 3000, hostPort: 3000, name: "grafana-http", scheme: "http");

builder.AddProject<Projects.MetricsApp>("app")
       .WithEnvironment("GRAFANA_URL", () => grafana.GetEndpoint("grafana-http")?.UriString ?? $"{{{grafana.Resource.Name}.bindings.grafana-http}}");

builder.AddContainer("prometheus", "prom/prometheus")
       .WithVolumeMount("../prometheus", "/etc/prometheus")
       .WithServiceBinding(9090, hostPort: 9090);

using var app = builder.Build();

await app.RunAsync();
