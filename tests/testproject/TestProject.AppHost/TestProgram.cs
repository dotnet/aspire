// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
//using Aspire.Hosting.Dcp;
using Aspire.Hosting.Lifecycle;

public class TestProgram : IDisposable
{
    private TestProgram(string[] args, Assembly assembly, bool includeIntegrationServices, bool includeNodeApp, bool disableDashboard)
    {
        List<string> componentsToSkip = new();
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i].StartsWith("--skip-components", StringComparison.InvariantCultureIgnoreCase))
            {
                if (args.Length > i + 1)
                {
                    componentsToSkip.AddRange(args[i + 1].Split(','));
                }
                else
                {
                    throw new ArgumentException("Missing argument to --skip-components option.");
                }
            }
        }
        if (componentsToSkip.Contains("dashboard", StringComparer.OrdinalIgnoreCase))
        {
            disableDashboard = true;
        }

        AppBuilder = DistributedApplication.CreateBuilder(new DistributedApplicationOptions { Args = args, DisableDashboard = disableDashboard, AssemblyName = assembly.FullName });

        var serviceAPath = Path.Combine(Projects.TestProject_AppHost.ProjectPath, @"..\TestProject.ServiceA\TestProject.ServiceA.csproj");

        ServiceABuilder = AppBuilder.AddProject("servicea", serviceAPath);
        ServiceBBuilder = AppBuilder.AddProject<Projects.ServiceB>("serviceb");
        ServiceCBuilder = AppBuilder.AddProject<Projects.ServiceC>("servicec");
        WorkerABuilder = AppBuilder.AddProject<Projects.WorkerA>("workera");

        if (includeNodeApp)
        {
            // Relative to this project so that it doesn't changed based on
            // where this code is referenced from.
            var path = Path.Combine(Projects.TestProject_AppHost.ProjectPath, @"..\nodeapp");
            var scriptPath = Path.Combine(path, "app.js");

            NodeAppBuilder = AppBuilder.AddNodeApp("nodeapp", scriptPath)
                .WithHttpEndpoint(hostPort: 5031, env: "PORT");

            NpmAppBuilder = AppBuilder.AddNpmApp("npmapp", path)
                .WithHttpEndpoint(hostPort: 5032, env: "PORT");
        }

        if (includeIntegrationServices)
        {
            IntegrationServiceABuilder = AppBuilder.AddProject<Projects.IntegrationServiceA>("integrationservicea");
            IntegrationServiceABuilder = IntegrationServiceABuilder.WithEnvironment("SKIP_COMPONENTS", string.Join(',', componentsToSkip));

            if (!componentsToSkip.Contains("sqlserver", StringComparer.OrdinalIgnoreCase))
            {
                var sqlserverDbName = "tempdb";
                var sqlserver = AppBuilder.AddSqlServer("sqlserver")
                    .AddDatabase(sqlserverDbName);
                IntegrationServiceABuilder = IntegrationServiceABuilder.WithReference(sqlserver);
            }
            if (!componentsToSkip.Contains("mysql", StringComparer.OrdinalIgnoreCase))
            {
                var mysqlDbName = "mysqldb";
                var mysql = AppBuilder.AddMySql("mysql")
                    .WithEnvironment("MYSQL_DATABASE", mysqlDbName)
                    .AddDatabase(mysqlDbName);
                IntegrationServiceABuilder = IntegrationServiceABuilder.WithReference(mysql);
            }
            if (!componentsToSkip.Contains("redis", StringComparer.OrdinalIgnoreCase))
            {
                var redis = AppBuilder.AddRedis("redis");
                IntegrationServiceABuilder = IntegrationServiceABuilder.WithReference(redis);
            }
            if (!componentsToSkip.Contains("postgres", StringComparer.OrdinalIgnoreCase))
            {
                var postgresDbName = "postgresdb";
                var postgres = AppBuilder.AddPostgres("postgres")
                    .WithEnvironment("POSTGRES_DB", postgresDbName)
                    .AddDatabase(postgresDbName);
                IntegrationServiceABuilder = IntegrationServiceABuilder.WithReference(postgres);
            }
            if (!componentsToSkip.Contains("rabbitmq", StringComparer.OrdinalIgnoreCase))
            {
                var rabbitmq = AppBuilder.AddRabbitMQ("rabbitmq");
                IntegrationServiceABuilder = IntegrationServiceABuilder.WithReference(rabbitmq);
            }
            if (!componentsToSkip.Contains("mongodb", StringComparer.OrdinalIgnoreCase))
            {
                var mongoDbName = "mymongodb";
                var mongodb = AppBuilder.AddMongoDB("mongodb")
                    .AddDatabase(mongoDbName);
                IntegrationServiceABuilder = IntegrationServiceABuilder.WithReference(mongodb);
            }
            if (!componentsToSkip.Contains("oracledatabase", StringComparer.OrdinalIgnoreCase))
            {
                var oracleDbName = "freepdb1";
                var oracleDatabase = AppBuilder.AddOracleDatabase("oracledatabase")
                    .AddDatabase(oracleDbName);
                IntegrationServiceABuilder = IntegrationServiceABuilder.WithReference(oracleDatabase);
            }
            if (!componentsToSkip.Contains("kafka", StringComparer.OrdinalIgnoreCase))
            {
                var kafka = AppBuilder.AddKafka("kafka");
                IntegrationServiceABuilder = IntegrationServiceABuilder.WithReference(kafka);
            }
            if (!componentsToSkip.Contains("cosmos", StringComparer.OrdinalIgnoreCase))
            {
                var cosmos = AppBuilder.AddAzureCosmosDB("cosmos").UseEmulator();
                IntegrationServiceABuilder = IntegrationServiceABuilder.WithReference(cosmos);
            }
        }

        AppBuilder.Services.AddLifecycleHook<EndPointWriterHook>();
    }

    public static TestProgram Create<T>(string[]? args = null, bool includeIntegrationServices = false, bool includeNodeApp = false, bool disableDashboard = true) =>
        new TestProgram(args ?? [], typeof(T).Assembly, includeIntegrationServices, includeNodeApp, disableDashboard);

    public IDistributedApplicationBuilder AppBuilder { get; private set; }
    public IResourceBuilder<ProjectResource> ServiceABuilder { get; private set; }
    public IResourceBuilder<ProjectResource> ServiceBBuilder { get; private set; }
    public IResourceBuilder<ProjectResource> ServiceCBuilder { get; private set; }
    public IResourceBuilder<ProjectResource> WorkerABuilder { get; private set; }
    public IResourceBuilder<ProjectResource>? IntegrationServiceABuilder { get; private set; }
    public IResourceBuilder<NodeAppResource>? NodeAppBuilder { get; private set; }
    public IResourceBuilder<NodeAppResource>? NpmAppBuilder { get; private set; }
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

    public void Run() => Build().Run();

    public void Dispose() => App?.Dispose();

    /// <summary>
    /// Writes the allocated endpoints to the console in JSON format.
    /// This allows for easier consumption by the external test process.
    /// </summary>
    private sealed class EndPointWriterHook : IDistributedApplicationLifecycleHook
    {
        public async Task AfterEndpointsAllocatedAsync(DistributedApplicationModel appModel, CancellationToken cancellationToken)
        {
            var root = new JsonObject();
            foreach (var project in appModel.Resources.OfType<ProjectResource>())
            {
                var projectJson = new JsonObject();
                root[project.Name] = projectJson;

                var endpointsJsonArray = new JsonArray();
                projectJson["Endpoints"] = endpointsJsonArray;

                foreach (var endpoint in project.Annotations.OfType<AllocatedEndpointAnnotation>())
                {
                    var endpointJsonObject = new JsonObject();
                    endpointJsonObject["Name"] = endpoint.Name;
                    endpointJsonObject["Uri"] = endpoint.UriString;
                    endpointsJsonArray.Add(endpointJsonObject);
                }
            }

            // write the whole json in a single line so it's easier to parse by the external process
            await Console.Out.WriteLineAsync("$ENDPOINTS: " + JsonSerializer.Serialize(root, JsonSerializerOptions.Default));
        }
    }
}

