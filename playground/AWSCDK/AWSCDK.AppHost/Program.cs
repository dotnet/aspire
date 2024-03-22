using Amazon;
using AWSCDK.AppHost;

var builder = DistributedApplication.CreateBuilder(args);

// Setup a configuration for the AWS .NET SDK.
var awsConfig = builder.AddAWSSDKConfig()
    .WithProfile("default")
    .WithRegion(RegionEndpoint.EUWest1);

var stack = builder.AddAWSCDKStack(
        "AspireWebAppStack",
        app => new WebAppStack(app, "AspireWebAppStack", new WebAppStackProps()))
    .WithOutput("TableName", stack => stack.Table.TableName)
    .WithReference(awsConfig);

builder.AddProject<Projects.WebApp>("webapp")
    .WithReference(stack)
    .WithReference(awsConfig);

builder.Build().Run();
