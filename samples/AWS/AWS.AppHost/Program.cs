// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Amazon;

var builder = DistributedApplication.CreateBuilder(args);

var awsConfig = builder.AddAWSSDKConfig("default")
                        .WithRegion(RegionEndpoint.USEast2);

var awsResource = builder.AddAWSCloudFormationProvisioning("AspireSampleDevResources", "app-resources.template")
                        .WithAWSSDKReference(awsConfig);

builder.AddProject<Projects.Frontend>("frontend")
        .WithAWSCloudFormationReference(awsResource)
        .WithAWSSDKReference(awsConfig);

builder.Build().Run();
