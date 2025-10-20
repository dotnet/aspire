// To run this app in this repo use the following command line to ensure latest changes are always picked up:
// $ dotnet apphost.cs --no-cache

// These directives are not required in regular apps, only here in the aspire repo itself
#:property IsAspireHost=true
#:property PublishAot=false

var builder = DistributedApplication.CreateBuilder(args);

// C# File-based app
// NOTE: This is in a sub-folder to ensure it doesn't pickup .razor files from the FrontEnd project
builder.AddCSharpApp("api", "./api/api.cs");

// Traditional C# project added via same API just specifiying project directory
builder.AddCSharpApp("frontend", "./FileBasedApps.WebFrontEnd/");

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
