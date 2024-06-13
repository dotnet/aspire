using Amazon;

var builder = DistributedApplication.CreateBuilder(args);

// Setup a configuration for the AWS .NET SDK.
var awsConfig = builder.AddAWSSDKConfig()
    .WithProfile("default")
    .WithRegion(RegionEndpoint.EUWest1);

var cdk = builder.AddAWSCDK("cdk", "AspireStack").WithReference(awsConfig);

var topic = cdk.AddSNSTopic("topic");
var queue = cdk.AddSQSQueue("queue");
topic.AddSubscription(queue);

builder.AddProject<Projects.Frontend>("frontend")
    .WithEnvironment("AWS__Resources__ChatTopicArn", topic, t => t.TopicArn);

builder.Build().Run();
