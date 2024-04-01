// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Amazon;

var builder = DistributedApplication.CreateBuilder(args);

// Setup a configuration for the AWS .NET SDK.
var awsConfig = builder.AddAWSSDKConfig()
                        .WithProfile("default")
                        .WithRegion(RegionEndpoint.USWest2);

// Provision application level resources like SQS queues and SNS topics defined in the CloudFormation template file app-resources.template.
var awsResources = builder.AddAWSCloudFormationTemplate("AspireSampleDevResources", "app-resources.template")
                        .WithParameter("DefaultVisibilityTimeout", "30")
                        // Add the SDK configuration so the AppHost knows what account/region to provision the resources.
                        .WithReference(awsConfig);

// To add outputs of a CloudFormation stack that was created outside of AppHost use the AddAWSCloudFormationStack method.
// then attach the CloudFormation resource to a project using the WithReference method.
//var awsExistingResource = builder.AddAWSCloudFormationStack("ExistingStackName")
//                        .WithReference(awsConfig);

builder.AddProject<Projects.Frontend>("Frontend")
       .WithExternalHttpEndpoints()
        // Demonstrating binding all of the output variables to a section in IConfiguration. By default they are bound to the AWS::Resources prefix.
        // The prefix is configurable by the optional configSection parameter.
        .WithReference(awsResources)
        // Demonstrating binding a single output variable to environment variable in the project.
        .WithEnvironment("ChatTopicArnEnv", awsResources.GetOutput("ChatTopicArn"))
        // Assign the SDK config to the project. The service clients created in the project relying on environment config
        // will pick up these configuration.
        .WithReference(awsConfig);

builder.Build().Run();
