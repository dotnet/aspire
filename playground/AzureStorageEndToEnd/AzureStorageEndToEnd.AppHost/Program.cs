// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

var builder = DistributedApplication.CreateBuilder(args);

var storage = builder.AddAzureStorage("storage").RunAsEmulator(container =>
{
    container.WithDataBindMount();
});

var blobs = storage.AddBlobs("blobs");
blobs.AddBlobContainer("mycontainer1", blobContainerName: "test-container-1");
blobs.AddBlobContainer("mycontainer2", blobContainerName: "test-container-2");

var queues = storage.AddQueues("queues");

var storage2 = builder.AddAzureStorage("storage2").RunAsEmulator(container =>
{
    container.WithDataBindMount();
});
var blobs2 = storage2.AddBlobs("blobs2");
var blobContainer2 = blobs2.AddBlobContainer("foocontainer", blobContainerName: "foo-container");

builder.AddProject<Projects.AzureStorageEndToEnd_ApiService>("api")
       .WithExternalHttpEndpoints()
       .WithReference(blobs).WaitFor(blobs)
       .WithReference(blobContainer2).WaitFor(blobContainer2)
       .WithReference(queues).WaitFor(queues);

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

