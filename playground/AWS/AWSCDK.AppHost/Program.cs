using Amazon;

var builder = DistributedApplication.CreateBuilder(args);

// Setup a configuration for the AWS .NET SDK.
var awsConfig = builder.AddAWSSDKConfig()
    .WithProfile("vinles+labs-Admin")
    .WithRegion(RegionEndpoint.EUWest1);

var cdk = builder.AddAWSCDK("app");
var stack = cdk.AddStack("stack", "AspireStack").WithReference(awsConfig);

var topic = stack.AddSNSTopic("topic");
var queue = stack.AddSQSQueue("queue");
topic.AddSubscription(queue);

builder.AddProject<Projects.Frontend>("frontend")
    .WithEnvironment("AWS__Resources__ChatTopicArn", topic, t => t.TopicArn);

builder.Build().Run();
