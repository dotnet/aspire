// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Amazon;

var builder = DistributedApplication.CreateBuilder(args);

var awsResource = builder.AddAWSCloudFormationProvisioning("AspireSampleDevResources", "app-resources.template")
                        .WithAWSRegion(RegionEndpoint.USEast2);

builder.AddProject<Projects.Frontend>("frontend")
        .WithAWSCloudFormationReference(awsResource)
        .WithAWSRegion(RegionEndpoint.USEast2);

builder.Build().Run();
