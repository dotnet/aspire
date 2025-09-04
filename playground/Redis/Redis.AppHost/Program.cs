var builder = DistributedApplication.CreateBuilder(args);

#pragma warning disable ASPIREAZUREREDIS001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
var redis = builder.AddAzureRedisEnterprise("redis");
#pragma warning restore ASPIREAZUREREDIS001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

var garnet = builder.AddGarnet("garnet")
    .WithDataVolume();

var valkey = builder.AddValkey("valkey")
    .WithDataVolume("valkey-data");

builder.AddProject<Projects.Redis_ApiService>("apiservice")
    .WithExternalHttpEndpoints()
    .WithReference(redis).WaitFor(redis)
    .WithReference(garnet).WaitFor(garnet)
    .WithReference(valkey).WaitFor(valkey);

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
