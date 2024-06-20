using Amazon;
using AWSCDK.AppHost;

var builder = DistributedApplication.CreateBuilder(args);

// Setup a configuration for the AWS .NET SDK.
var awsConfig = builder.AddAWSSDKConfig()
    .WithProfile("default")
    .WithRegion(RegionEndpoint.EUWest1);

var cdk = builder.AddAWSCDK("cdk", "AspireStack").WithReference(awsConfig);

// Adds a custom stack and reference constructs as output
var stack = cdk.AddStack("stack", scope => new CustomStack(scope, "AspireStack-stack"));
stack.AddOutput("BucketName", s => s.Bucket.BucketName);

var topic = cdk.AddSNSTopic("topic");
var queue = cdk.AddSQSQueue("queue");
topic.AddSubscription(queue);

builder.AddProject<Projects.Frontend>("frontend")
    //.WithReference(stack) // Reference all outputs of a construct
    .WithEnvironment("AWS__Resources__BucketName", stack.GetOutput("BucketName")) // Reference a construct/stack output
    .WithEnvironment("AWS__Resources__ChatTopicArn", topic, t => t.TopicArn);

builder.Build().Run();
