// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Amazon;

var builder = DistributedApplication.CreateBuilder(args);

// Setup a configuration for the AWS .NET SDK.
var awsConfig = builder.AddAWSSDKConfig()
                        .WithProfile("default")
                        .WithRegion(RegionEndpoint.USEast2);

// Provision application level resources like SQS queues and SNS topics defined in the CloudFormation template file app-resources.template.
var awsResources = builder.AddAWSCloudFormationTemplate("AspireSampleDevResources", "app-resources.template")
                        // Add the SDK configuration so the AppHost knows what account/region to provision the resources.
                        .WithReference(awsConfig);

builder.AddProject<Projects.Frontend>("frontend")
        // Reference the CloudFormation resource to project. The output parameters will added to the IConfiguration of the project.
        .WithReference(awsResources)
        // Assign the SDK config to the project. The service clients created in the project relying on environment config
        // will pick up these configuration.
        .WithReference(awsConfig);

builder.Build().Run();
