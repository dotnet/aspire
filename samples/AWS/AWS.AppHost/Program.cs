// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using AWS.AppHost;

var builder = DistributedApplication.CreateBuilder(args);

// Startup the DynamoDB Local container
var dynamoDB = builder
                    .AddAWSDynamoDBLocal("default")
                    // Add a callback to seed the DynamoDB local with sample data.
                    .WithSeedDynamoDBCallback(DynamoDBLocalLoader.Configure);

// The WithDynamoDBLocalReference sets up Dynamodb endpoint environment variable
// on the project pointing the local DynamoDB so the AWS .NET SDK picks that up as the
// endpoint.
builder.AddProject<Projects.Frontend>("frontend")
        .WithAWSDynamoDBLocalReference(dynamoDB)
        .WithEnvironment("ZIP_CODE_TABLE", DynamoDBLocalLoader.ZIP_CODE_TABLENAME);

builder.Build().Run();
