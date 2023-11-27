// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

var builder = DistributedApplication.CreateBuilder(args);

builder.AddAwsProvisioning();
var awsS3Bucket = builder.AddAwsS3Bucket("ProfilePicturesBucket");
var awsSnsTopic = builder.AddAwsSnsTopic("ProfilesTopic");
var awsSqsQueue = builder.AddAwsSqsQueue("ProfilesQueue");

builder.AddProject<Projects.Aws_UserService>("aws.userservice")
    .WithReference(awsS3Bucket)
    .WithReference(awsSnsTopic)
    .WithReference(awsSqsQueue);

builder.Build().Run();
