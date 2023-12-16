// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Amazon;

var builder = DistributedApplication.CreateBuilder(args);

var awsResource = builder.AddAWSCloudFormationProvisioning("AspireSampleDevResources", "app-resources.template")
                        .WithAWSRegion(RegionEndpoint.USWest2)
                        .WithAWSProfile("not-gonna-do-it");

builder.AddProject<Projects.Frontend>("frontend")
        .WithAWSCloudFormationReference(awsResource, "CloudResources");

builder.Build().Run();
