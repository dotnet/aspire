// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;

public class TestProgram
{
    private TestProgram(string[] args, Assembly assembly, bool includeIntegrationServices = false, bool disableDashboard = true, bool includeNodeApp = false)
    {
        AppBuilder = DistributedApplication.CreateBuilder(new DistributedApplicationOptions { Args = args, DisableDashboard = disableDashboard, AssemblyName = assembly.FullName });
        ServiceABuilder = AppBuilder.AddProject<Projects.ServiceA>("servicea");
        ServiceBBuilder = AppBuilder.AddProject<Projects.ServiceB>("serviceb");
        ServiceCBuilder = AppBuilder.AddProject<Projects.ServiceC>("servicec");

        if (includeNodeApp)
        {
            // Relative to this project so that it doesn't changed based on
            // where this code is referenced from.
            var path = Path.Combine(Projects.TestProject_AppHost.ProjectPath, @"..\nodeapp");

            NodeApp = AppBuilder.AddNodeApp("nodeapp", path, ["app.js"])
                .WithServiceBinding(hostPort: 5031, scheme: "http", portEnvVar: "PORT");

            NpmApp = AppBuilder.AddNpmApp("npmapp", path)
                .WithServiceBinding(hostPort: 5032, scheme: "http", portEnvVar: "PORT");
        }

        if (includeIntegrationServices)
        {
            var sqlserver = AppBuilder.AddSqlServerContainer("sqlserver");
            var mysql = AppBuilder.AddMySqlContainer("mysql");
            var redis = AppBuilder.AddRedisContainer("redis");
            var postgres = AppBuilder.AddPostgresContainer("postgres");
            var rabbitmq = AppBuilder.AddRabbitMQContainer("rabbitmq");

            IntegrationServiceA = AppBuilder.AddProject<Projects.IntegrationServiceA>("integrationservicea")
                .WithReference(sqlserver)
                .WithReference(mysql)
                .WithReference(redis)
                .WithReference(postgres)
                .WithReference(rabbitmq);
        }
    }

    public static TestProgram Create<T>(string[]? args = null, bool includeIntegrationServices = false, bool includeNodeApp = false, bool disableDashboard = true) =>
        new TestProgram(args ?? [], typeof(T).Assembly, includeIntegrationServices, disableDashboard, includeNodeApp: includeNodeApp);

    public IDistributedApplicationBuilder AppBuilder { get; private set; }
    public IResourceBuilder<ProjectResource> ServiceABuilder { get; private set; }
    public IResourceBuilder<ProjectResource> ServiceBBuilder { get; private set; }
    public IResourceBuilder<ProjectResource> ServiceCBuilder { get; private set; }
    public IResourceBuilder<ProjectResource>? IntegrationServiceA { get; private set; }
    public IResourceBuilder<NodeAppResource>? NodeApp { get; private set; }
    public IResourceBuilder<NodeAppResource>? NpmApp { get; private set; }
    public DistributedApplication? App { get; private set; }

    public List<IResourceBuilder<ProjectResource>> ServiceProjectBuilders => [ServiceABuilder, ServiceBBuilder, ServiceCBuilder];

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        App = AppBuilder.Build();
        await App.RunAsync(cancellationToken);
    }

    public DistributedApplication Build()
    {
        if (App == null)
        {
            App = AppBuilder.Build();
        }
        return App;
    }

    public void Run()
    {
        Build();
        App!.Run();
    }
}

