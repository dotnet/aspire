// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

var builder = DistributedApplication.CreateBuilder(args);
builder.AddRedis("redis1");
builder.AddProject<Projects.TestingAppHost1_MyWebApp>("mywebapp1");
builder.AddProject<Projects.TestingAppHost1_MyWorker>("myworker1")
    .WithEndpoint(name: "myendpoint1");
builder.AddPostgres("postgres1");
builder.Build().Run();
