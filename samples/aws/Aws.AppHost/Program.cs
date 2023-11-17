// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

var builder = DistributedApplication.CreateBuilder(args);

builder.AddAwsProvisioning();
builder.AddAwsS3Bucket("ProfilePicturesBucket");

builder.AddProject<Projects.Aws_UserService>("aws.userservice");

builder.Build().Run();
