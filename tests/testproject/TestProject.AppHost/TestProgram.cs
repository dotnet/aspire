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
                .WithEndpoint(hostPort: 5031, scheme: "http", env: "PORT");

            NpmAppBuilder = AppBuilder.AddNpmApp("npmapp", path)
                .WithEndpoint(hostPort: 5032, scheme: "http", env: "PORT");
        }

        if (includeIntegrationServices)
        {
            var sqlserverDbName = "tempdb";
            var mysqlDbName = "mysqldb";
            var postgresDbName = "postgresdb";
            var mongoDbName = "mymongodb";
            var oracleDbName = "freepdb1";

            var sqlserverContainer = AppBuilder.AddSqlServerContainer("sqlservercontainer")
                .AddDatabase(sqlserverDbName);
            var mysqlContainer = AppBuilder.AddMySqlContainer("mysqlcontainer")
                .WithEnvironment("MYSQL_DATABASE", mysqlDbName)
                .AddDatabase(mysqlDbName);
            var redisContainer = AppBuilder.AddRedisContainer("rediscontainer");
            var postgresContainer = AppBuilder.AddPostgresContainer("postgrescontainer")
                .WithEnvironment("POSTGRES_DB", postgresDbName)
                .AddDatabase(postgresDbName);
            var rabbitmqContainer = AppBuilder.AddRabbitMQContainer("rabbitmqcontainer");
            var mongodbContainer = AppBuilder.AddMongoDBContainer("mongodbcontainer")
                .AddDatabase(mongoDbName);
            var oracleDatabaseContainer = AppBuilder.AddOracleDatabaseContainer("oracledatabasecontainer")
                .AddDatabase(oracleDbName);

            var sqlserverAbstract = AppBuilder.AddSqlServer("sqlserverabstract");
            var mysqlAbstract = AppBuilder.AddMySql("mysqlabstract");
            var redisAbstract = AppBuilder.AddRedis("redisabstract");
            var postgresAbstract = AppBuilder.AddPostgres("postgresabstract");
            var rabbitmqAbstract = AppBuilder.AddRabbitMQ("rabbitmqabstract");
            var mongodbAbstract = AppBuilder.AddMongoDB("mongodbabstract");
            var oracleDatabaseAbstract = AppBuilder.AddOracleDatabaseContainer("oracledatabaseabstract");

            IntegrationServiceABuilder = AppBuilder.AddProject<Projects.IntegrationServiceA>("integrationservicea")
                .WithReference(sqlserverContainer)
                .WithReference(mysqlContainer)
                .WithReference(redisContainer)
                .WithReference(postgresContainer)
                .WithReference(rabbitmqContainer)
                .WithReference(mongodbContainer)
                .WithReference(oracleDatabaseContainer)
                .WithReference(sqlserverAbstract)
                .WithReference(mysqlAbstract)
                .WithReference(redisAbstract)
                .WithReference(postgresAbstract)
                .WithReference(rabbitmqAbstract)
                .WithReference(mongodbAbstract)
                .WithReference(oracleDatabaseAbstract);
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

