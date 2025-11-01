using Microsoft.Extensions.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var pass = builder.AddParameter("pass", "p@ssw0rd1");

var cache = builder
    .AddRedis("cache")
    .WithPassword(pass);

var weatherapi = builder.AddProject<Projects.AspireWithNode_AspNetCoreApi>("weatherapi");

var frontend = builder.AddJavaScriptApp("frontend", "../NodeFrontend", "watch")
    .WithReference(weatherapi)
    .WaitFor(weatherapi)
    .WithReference(cache)
    .WaitFor(cache)
    .WithHttpEndpoint(env: "PORT")
    .WithExternalHttpEndpoints()
    .PublishAsDockerFile();

var launchProfile = builder.Configuration["DOTNET_LAUNCH_PROFILE"];

if (builder.Environment.IsDevelopment() && launchProfile == "https")
{
#pragma warning disable ASPIREEXTENSION001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    frontend
        .WithHttpsEndpoint(env: "HTTPS_PORT")
        .WithDeveloperCertificateKeyPair()
        .WithCertificateKeyPairConfiguration(ctx =>
        {
            ctx.EnvironmentVariables["HTTPS_CERT_FILE"] = ctx.CertificatePath;
            ctx.EnvironmentVariables["HTTPS_CERT_KEY_FILE"] = ctx.KeyPath;
            return Task.CompletedTask;
        })
        .WithEnvironment(ctx =>
        {
            var httpsEndpoint = frontend.GetEndpoint("https");
            ctx.EnvironmentVariables["HTTPS_REDIRECT_PORT"] = ReferenceExpression.Create($"{httpsEndpoint.Property(EndpointProperty.Port)}");
        });

#pragma warning restore ASPIREEXTENSION001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
}

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
