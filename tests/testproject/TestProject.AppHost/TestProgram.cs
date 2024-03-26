// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using Aspire.Hosting.Lifecycle;
using Aspire.TestProject;

public class TestProgram : IDisposable
{
    private TestProgram(string[] args, Assembly assembly, bool includeIntegrationServices, bool includeNodeApp, bool disableDashboard)
    {
        ISet<TestResourceNames>? resourcesToSkip = null;
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i].StartsWith("--skip-resources", StringComparison.InvariantCultureIgnoreCase))
            {
                if (args.Length > i + 1)
                {
                    resourcesToSkip = TestResourceNamesExtensions.Parse(args[i + 1].Split(','));
                    break;
                }
                else
                {
                    throw new ArgumentException("Missing argument to --skip-resources option.");
                }
            }
        }
        resourcesToSkip ??= new HashSet<TestResourceNames>();
        if (resourcesToSkip.Contains(TestResourceNames.dashboard))
        {
            disableDashboard = true;
        }

        AppBuilder = DistributedApplication.CreateBuilder(new DistributedApplicationOptions { Args = args, DisableDashboard = disableDashboard, AssemblyName = assembly.FullName });

        var serviceAPath = Path.Combine(Projects.TestProject_AppHost.ProjectPath, @"..\TestProject.ServiceA\TestProject.ServiceA.csproj");

        ServiceABuilder = AppBuilder.AddProject("servicea", serviceAPath, launchProfileName: "http");
        ServiceBBuilder = AppBuilder.AddProject<Projects.ServiceB>("serviceb", launchProfileName: "http");
        ServiceCBuilder = AppBuilder.AddProject<Projects.ServiceC>("servicec", launchProfileName: "http");
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
            IntegrationServiceABuilder = IntegrationServiceABuilder.WithEnvironment("SKIP_RESOURCES", string.Join(',', resourcesToSkip));

            if (!resourcesToSkip.Contains(TestResourceNames.sqlserver))
            {
                var sqlserverDbName = "tempdb";
                var sqlserver = AppBuilder.AddSqlServer("sqlserver")
                    .AddDatabase(sqlserverDbName);
                IntegrationServiceABuilder = IntegrationServiceABuilder.WithReference(sqlserver);
            }
            if (!resourcesToSkip.Contains(TestResourceNames.mysql))
            {
                var mysqlDbName = "mysqldb";
                var mysql = AppBuilder.AddMySql("mysql")
                    .WithEnvironment("MYSQL_DATABASE", mysqlDbName)
                    .AddDatabase(mysqlDbName);
                IntegrationServiceABuilder = IntegrationServiceABuilder.WithReference(mysql);
            }
            if (!resourcesToSkip.Contains(TestResourceNames.redis))
            {
                var redis = AppBuilder.AddRedis("redis");
                IntegrationServiceABuilder = IntegrationServiceABuilder.WithReference(redis);
            }
            if (!resourcesToSkip.Contains(TestResourceNames.postgres))
            {
                var postgresDbName = "postgresdb";
                var postgres = AppBuilder.AddPostgres("postgres")
                    .WithEnvironment("POSTGRES_DB", postgresDbName)
                    .AddDatabase(postgresDbName);
                IntegrationServiceABuilder = IntegrationServiceABuilder.WithReference(postgres);
            }
            if (!resourcesToSkip.Contains(TestResourceNames.rabbitmq))
            {
                var rabbitmq = AppBuilder.AddRabbitMQ("rabbitmq");
                IntegrationServiceABuilder = IntegrationServiceABuilder.WithReference(rabbitmq);
            }
            if (!resourcesToSkip.Contains(TestResourceNames.mongodb))
            {
                var mongoDbName = "mymongodb";
                var mongodb = AppBuilder.AddMongoDB("mongodb")
                    .AddDatabase(mongoDbName);
                IntegrationServiceABuilder = IntegrationServiceABuilder.WithReference(mongodb);
            }
            if (!resourcesToSkip.Contains(TestResourceNames.oracledatabase))
            {
                var oracleDbName = "freepdb1";
                var oracleDatabase = AppBuilder.AddOracle("oracledatabase")
                    .AddDatabase(oracleDbName);
                IntegrationServiceABuilder = IntegrationServiceABuilder.WithReference(oracleDatabase);
            }
            if (!resourcesToSkip.Contains(TestResourceNames.kafka))
            {
                var kafka = AppBuilder.AddKafka("kafka");
                IntegrationServiceABuilder = IntegrationServiceABuilder.WithReference(kafka);
            }
            if (!resourcesToSkip.Contains(TestResourceNames.cosmos))
            {
                var cosmos = AppBuilder.AddAzureCosmosDB("cosmos").RunAsEmulator();
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

                foreach (var endpoint in project.Annotations.OfType<EndpointAnnotation>())
                {
                    var allocatedEndpoint = endpoint.AllocatedEndpoint;
                    if (allocatedEndpoint is null)
                    {
                        continue;
                    }

                    var endpointJsonObject = new JsonObject
                    {
                        ["Name"] = endpoint.Name,
                        ["Uri"] = allocatedEndpoint.UriString
                    };
                    endpointsJsonArray.Add(endpointJsonObject);
                }
            }

            // write the whole json in a single line so it's easier to parse by the external process
            await Console.Out.WriteLineAsync("$ENDPOINTS: " + JsonSerializer.Serialize(root, JsonSerializerOptions.Default));
        }
    }
}

