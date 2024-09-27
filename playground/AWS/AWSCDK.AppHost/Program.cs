using Amazon;
using AWSCDK.AppHost;

var builder = DistributedApplication.CreateBuilder(args);

// Setup a configuration for the AWS .NET SDK.
var awsConfig = builder.AddAWSSDKConfig()
    .WithProfile("default")
    .WithRegion(RegionEndpoint.EUWest1);

var stack = builder.AddAWSCDKStack("stack", "Aspire-stack").WithReference(awsConfig);
var customStack = builder.AddAWSCDKStack("custom", scope => new CustomStack(scope, "Aspire-custom"));
customStack.AddOutput("BucketName", stack => stack.Bucket.BucketName).WithReference(awsConfig);

var topic = stack.AddSNSTopic("topic");
var queue = stack.AddSQSQueue("queue");
topic.AddSubscription(queue);

builder.AddProject<Projects.Frontend>("frontend")
    //.WithReference(stack) // Reference all outputs of a construct
    .WithEnvironment("AWS__Resources__BucketName", customStack.GetOutput("BucketName")) // Reference a construct/stack output
    .WithEnvironment("AWS__Resources__ChatTopicArn", topic, t => t.TopicArn)
    .WithReference(customStack, s => s.Queue.QueueUrl, "QueueUrl", "AWS:Resources:Queue");

builder.Build().Run();
