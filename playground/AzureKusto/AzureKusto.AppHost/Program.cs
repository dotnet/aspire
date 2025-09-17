using Aspire.Hosting.Azure.Kusto;
using Kusto.Data;
using Kusto.Data.Net.Client;

var builder = DistributedApplication.CreateBuilder(args);

builder.AddAzureContainerAppEnvironment("infra");

var kusto = builder.AddAzureKustoCluster("kusto")
    .RunAsEmulator();
var db = kusto.AddReadWriteDatabase("testdb");

builder.AddProject<Projects.AzureKusto_Worker>("worker")
    .WithReference(db)
    .WaitFor(db);

// Option 1: Seed as part of AppHost startup
// Works well for local development and Aspire-based testing, but doesn't support seeding
// in production or other scenarios.
db.OnResourceReady(async (dbResource, evt, ct) =>
{
    if (!kusto.Resource.IsEmulator)
    {
        Console.WriteLine("Skipping Kusto DB seeding for non emulator.");
        return;
    }

    var connectionString = await dbResource.ConnectionStringExpression.GetValueAsync(ct);
    var kcsb = new KustoConnectionStringBuilder(connectionString);

    var admin = KustoClientFactory.CreateCslAdminProvider(kcsb);

    const string command =
        """
        .execute database script with (ThrowOnErrors=true) <|
            .create-merge table TestTable (Id: int, Name: string, Timestamp: datetime)
            .ingest inline into table TestTable <|
                1,"Alice",datetime(2024-01-01T10:00:00Z)
                2,"Bob",datetime(2024-01-01T11:00:00Z)
                3,"Charlie",datetime(2024-01-01T12:00:00Z)
        """;

    await admin.ExecuteControlCommandAsync(admin.DefaultDatabaseName, command);
});

#if !SKIP_DASHBOARD_REFERENCE
// This project is only added in playground projects to support development/debugging
// of the dashboard. It is not required in end developer code. Comment out this code
// or build with `/p:SkipDashboardReference=true`, to test end developer
// dashboard launch experience, Refer to Directory.Build.props for the path to
// the dashboard binary (defaults to the Aspire.Dashboard bin output in the
// artifacts dir).
builder.AddProject<Projects.Aspire_Dashboard>(KnownResourceNames.AspireDashboard);
#endif

var app = builder.Build();
app.Run();
