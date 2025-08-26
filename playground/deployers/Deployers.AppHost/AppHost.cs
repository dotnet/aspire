#pragma warning disable ASPIRECOMPUTE001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

var builder = DistributedApplication.CreateBuilder(args);

var aca = builder.AddAzureContainerAppEnvironment("aca-env");
var aas = builder.AddAzureAppServiceEnvironment("aas-env");

var storage = builder.AddAzureStorage("storage");

storage.AddBlobs("blobs");
storage.AddBlobContainer("mycontainer1", blobContainerName: "test-container-1");
storage.AddBlobContainer("mycontainer2", blobContainerName: "test-container-2");
storage.AddQueue("myqueue", queueName: "my-queue");

builder.AddRedis("cache")
    .WithComputeEnvironment(aca);

builder.AddProject<Projects.Deployers_ApiService>("api-service")
    .WithExternalHttpEndpoints()
    .WithComputeEnvironment(aas);

builder.AddDockerfile("python-app", "../Deployers.Dockerfile")
    .WithHttpEndpoint(targetPort: 80)
    .WithExternalHttpEndpoints()
    .WithComputeEnvironment(aca);

#if !SKIP_DASHBOARD_REFERENCE
// This project is only added in playground projects to support development/debugging
// of the dashboard. It is not required in end developer code. Comment out this code
// or build with `/p:SkipDashboardReference=true`, to test end developer
// dashboard launch experience, Refer to Directory.Build.props for the path to
// the dashboard binary (defaults to the Aspire.Dashboard bin output in the
// artifacts dir).
builder.AddProject<Projects.Aspire_Dashboard>(KnownResourceNames.AspireDashboard);
#endif

builder.Build().Run();
