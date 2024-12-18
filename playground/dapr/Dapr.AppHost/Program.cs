using Aspire.Hosting.Dapr;
using Aspire.Hosting.Dapr.Models.ComponentSpec;

var builder = DistributedApplication.CreateBuilder(args);

var rmq = builder.AddRabbitMQ("rabbitMQ")
                   .WithManagementPlugin()
                   .WithEndpoint("tcp", e => e.Port = 5672)
                   .WithEndpoint("management", e => e.Port = 15672);

var secretStore = builder.AddDaprComponent("secrets", "secretstores.local.file", new(){
   Configuration = new List<MetadataValue>
       {
              new MetadataDirectValue<string> { Name = "secretsFile", Value = "appsettings.json"},
              new MetadataDirectValue<string> { Name = "nestedSeparator", Value = ":"},
              new MetadataDirectValue<bool> { Name = "multiValued", Value = false}
       }
});

var stateStore = builder.AddDaprStateStore("statestore", options: new DaprComponentOptions
{
       Configuration = new List<MetadataValue>
              {
                     new MetadataDirectValue<string> { Name = "actorStateStore", Value = "true" },
                     new MetadataSecretValue { Name = "logginglevel", SecretKeyRef = new("Logging:LogLevel:Default")}
              },
       SecretStore = secretStore.Resource
}
);
var pubSub = builder.AddDaprPubSub("pubsub", "rabbitmq", options: new DaprComponentOptions
{
       Configuration = new List<MetadataValue>
       {
              new MetadataDirectValue<int> { Name = "reconnectWait", Value = 2 },
       }
})
       .WaitFor(rmq)
       .WithReference(rmq);

builder.AddProject<Projects.DaprServiceA>("servicea")
       .WithDaprSidecar()
       .WithReference(stateStore)
       .WithReference(pubSub)
       .WithReference(secretStore);

builder.AddProject<Projects.DaprServiceB>("serviceb")
       .WithDaprSidecar()
       .WithReference(pubSub);

// console app with no appPort (sender only)
builder.AddProject<Projects.DaprServiceC>("servicec")
       .WithDaprSidecar()
       .WithReference(stateStore)
       .WithReference(secretStore);

#if !SKIP_DASHBOARD_REFERENCE
// This project is only added in playground projects to support development/debugging
// of the dashboard. It is not required in end developer code. Comment out this code
// or build with `/p:SkipDashboardReference=true`, to test end developer
// dashboard launch experience, Refer to Directory.Build.props for the path to
// the dashboard binary (defaults to the Aspire.Dashboard bin output in the
// artifacts dir).
builder.AddProject<Projects.Aspire_Dashboard>(KnownResourceNames.AspireDashboard);
#endif

using var app = builder.Build();

await app.RunAsync();
