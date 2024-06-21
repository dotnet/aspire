# Aspire.Hosting.AWS library

Provides extension methods and resources definition for a .NET Aspire AppHost to configure the AWS SDK for .NET and AWS application resources.

## Prerequisites

- [Configure AWS credentials](https://docs.aws.amazon.com/cli/latest/userguide/cli-configure-files.html)
- [Node.js](https://nodejs.org) _(AWS CDK only)_

## Install the package

In your AppHost project, install the `Aspire.Hosting.AWS` library with [NuGet](https://www.nuget.org):

```dotnetcli
dotnet add package Aspire.Hosting.AWS
```

## Configuring the AWS SDK for .NET

The AWS profile and region the SDK should use can be configured using the `AddAWSSDKConfig` method.
The following example creates a config using the dev profile from the `~/.aws/credentials` file and points the SDK to the
`us-west-2` region.

```csharp
var awsConfig = builder.AddAWSSDKConfig()
                        .WithProfile("dev")
                        .WithRegion(RegionEndpoint.USWest2);
```

The configuration can be attached to projects using the `WithReference` method. This will set the `AWS_PROFILE` and `AWS_REGION`
environment variables on the project to the profile and region configured by the `AddAWSSDKConfig` method. SDK service clients created in the
project without explicitly setting the credentials and region will pick up these environment variables and use them
to configure the service client.

```csharp
builder.AddProject<Projects.Frontend>("Frontend")
        .WithReference(awsConfig)
```

If a project has a reference to an AWS resource like the AWS CloudFormation resources that have an AWS SDK configuration
the project will infer the AWS SDK configuration from the AWS resource. For example if you call the `WithReference` passing
in the CloudFormation resource then a second `WithReference` call passing in the AWS SDK configuration is not necessary.

## Provisioning application resources with AWS CloudFormation

AWS application resources like Amazon DynamoDB tables or Amazon Simple Queue Service (SQS) queues can be provisioned during AppHost
startup using a CloudFormation template.

In the AppHost project create either a JSON or YAML CloudFormation template. Here is an example template called `app-resources.template` that creates a queue and topic.
```json
{
    "AWSTemplateFormatVersion" : "2010-09-09",
    "Parameters" : {
        "DefaultVisibilityTimeout" : {
            "Type" : "Number",
            "Description" : "The default visiblity timeout for messages in SQS queue."
        }
    },
    "Resources" : {
        "ChatMessagesQueue" : {
            "Type" : "AWS::SQS::Queue",
            "Properties" : {
                "VisibilityTimeout" : { "Ref" : "DefaultVisibilityTimeout" }
            }
        },
        "ChatTopic" : {
            "Type" : "AWS::SNS::Topic",
            "Properties" : {
                "Subscription" : [
                    { "Protocol" : "sqs", "Endpoint" : { "Fn::GetAtt" : [ "ChatMessagesQueue", "Arn" ] } }
                ]
            }
        }
    },
    "Outputs" : {
        "ChatMessagesQueueUrl" : {
            "Value" : { "Ref" : "ChatMessagesQueue" }
        },
        "ChatTopicArn" : {
            "Value" : { "Ref" : "ChatTopic" }
        }
    }
}
```

In the AppHost the `AddAWSCloudFormationTemplate` method is used to register the CloudFormation resource. The first parameter,
which is the Aspire resource name, is used as the CloudFormation stack name. If the template defines parameters the value can be provided using 
the `WithParameter` method. To configure what AWS account and region to deploy the CloudFormation stack,
the `WithReference` method is used to associate a SDK configuration.

```csharp
var awsResources = builder.AddAWSCloudFormationTemplate("AspireSampleDevResources", "app-resources.template")
                          .WithParameter("DefaultVisibilityTimeout", "30")
                          .WithReference(awsConfig);
```

The outputs of a CloudFormation stack can be associated to a project using the `WithReference` method.

```csharp
builder.AddProject<Projects.Frontend>("Frontend")
       .WithReference(awsResources);
```

The output parameters from the CloudFormation stack can be found in the `IConfiguration` under the `AWS:Resources` config section. The config section
can be changed by setting the `configSection` parameter of the `WithReference` method associating the CloudFormation stack to the project.

```csharp
var chatTopicArn = builder.Configuration["AWS:Resources:ChatTopicArn"];
```

Alternatively a single CloudFormation stack output parameter can be assigned to an environment variable using the `GetOutput` method.

```csharp
builder.AddProject<Projects.Frontend>("Frontend")
       .WithEnvironment("ChatTopicArnEnv", awsResources.GetOutput("ChatTopicArn"))
```

## Importing existing AWS resources

To import AWS resources that were created by a CloudFormation stack outside of the AppHost the `AddAWSCloudFormationStack` method can be used.
It will associated the outputs of the CloudFormation stack the same as the provisioning method `AddAWSCloudFormationTemplate`.

```csharp
var awsResources = builder.AddAWSCloudFormationStack("ExistingStackName")
                          .WithReference(awsConfig);

builder.AddProject<Projects.Frontend>("Frontend")
       .WithReference(awsResources);
```

## Provisioning application resources with AWS CDK

Adding [AWS CDK](https://aws.amazon.com/cdk/) to the AppHost makes it possible to provision AWS resources using code. Under the hood AWS CDK is using CloudFormation to create the resources in AWS.

In the AppHost the `AddAWSCDK` methods is used to create a CDK Resources which will hold the constructs for describing the AWS resources.

A number of methods are available to add common resources to the AppHost like S3 Buckets, DynamoDB Tables, SQS Queues, SNS Topics, Kinesis Streams and Cognito User Pools. These resources can be added either the CDK resource or a dedicated stack that can be created.

```csharp
var cdk = builder.AddAWSCDK("CDK");
var bucket = cdk.AddS3Bucket("Bucket");

builder.AddProject<Projects.Frontend>("Frontend")
       .WithReference(bucket);
```

Resources created with these methods can be directly referenced by project resources and common properies like resource names, ARNs or URLs will be made availabile as configuration environment variables. The default config section will be `AWS:Resources`

Alternative constructs can created in free form using the `AddConstruct` methods. These constructs can be references with the `WithReference` method and need to be provided with a property selector and an output name. This will make this property available as configuration environment variable

```csharp
var cdk = builder.AddAWSCDK("CDK");
var constuct = cdk.AddConstruct("Construct", scope => new CustomConstruct(scope, "Construct"));

builder.AddProject<Projects.Frontend>("Frontend")
       .WithReference(construct, c => c.Url, "Url");
```

## Feedback & contributing

https://github.com/dotnet/aspire
