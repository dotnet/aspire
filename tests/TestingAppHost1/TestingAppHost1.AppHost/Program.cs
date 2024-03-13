// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

var builder = DistributedApplication.CreateBuilder(args);
builder.AddRedis("redis1");
builder.AddProject<Projects.TestingAppHost1_MyWebApp>("mywebapp1");
builder.AddProject<Projects.TestingAppHost1_MyWorker>("myworker1")
    .WithEndpoint(name: "myendpoint1");
builder.AddPostgres("postgres1");
builder.Build().Run();

// Require a public Program class to reference this in the integration tests. Using IVT alone is not sufficient
// in this case, because the accessibility of the `Program` type must match that of the fixture class.

public partial class Program
{
}

