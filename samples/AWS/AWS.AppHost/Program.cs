// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

var builder = DistributedApplication.CreateBuilder(args);

var awsResource = builder.AddAWSCloudFormationProvisioning("AspireSampleDevResources", "app-resources.template");

builder.AddProject<Projects.Frontend>("frontend")
        .WithAWSCloudFormationReference(awsResource);

builder.Build().Run();
