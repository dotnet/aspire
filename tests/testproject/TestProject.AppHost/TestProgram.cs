// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;

public class TestProgram
{
    private TestProgram(string[] args, Assembly assembly, bool includeIntegrationServices = false, bool disableDashboard = true, bool includeNodeApp = false)
    {
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
            var sqlserverDbName = "tempdb";
            var mysqlDbName = "mysqldb";
            var postgresDbName = "postgresdb";
            var mongoDbName = "mymongodb";
            var oracleDbName = "freepdb1";

            var sqlserver = AppBuilder.AddSqlServer("sqlserver")
                .AddDatabase(sqlserverDbName);
            var mysql = AppBuilder.AddMySql("mysql")
                .WithEnvironment("MYSQL_DATABASE", mysqlDbName)
                .AddDatabase(mysqlDbName);
            var redis = AppBuilder.AddRedis("redis");
            var postgres = AppBuilder.AddPostgres("postgres")
                .WithEnvironment("POSTGRES_DB", postgresDbName)
                .AddDatabase(postgresDbName);
            var rabbitmq = AppBuilder.AddRabbitMQ("rabbitmq");
            var mongodb = AppBuilder.AddMongoDB("mongodb")
                .AddDatabase(mongoDbName);
            var oracleDatabase = AppBuilder.AddOracleDatabase("oracledatabase")
                .AddDatabase(oracleDbName);
            var kafka = AppBuilder.AddKafka("kafka");
            var cosmos = AppBuilder.AddAzureCosmosDB("cosmos").UseEmulator();

            IntegrationServiceABuilder = AppBuilder.AddProject<Projects.IntegrationServiceA>("integrationservicea")
                .WithReference(sqlserver)
                .WithReference(mysql)
                .WithReference(redis)
                .WithReference(postgres)
                .WithReference(rabbitmq)
                .WithReference(mongodb)
                .WithReference(oracleDatabase)
                .WithReference(kafka)
                .WithReference(cosmos);
        }
    }

    public static TestProgram Create<T>(string[]? args = null, bool includeIntegrationServices = false, bool includeNodeApp = false, bool disableDashboard = true) =>
        new TestProgram(args ?? [], typeof(T).Assembly, includeIntegrationServices, disableDashboard, includeNodeApp: includeNodeApp);

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

    public void Run()
    {
        Build();
        App!.Run();
    }
}

