// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

var builder = DistributedApplication.CreateBuilder(args);
builder.Configuration["Parameters:pass"] = "123456";
var password = builder.AddParameter("pass");

var redis = builder.AddRedis("redis", password: password)
    .WithRedisCommander();

builder.AddProject<Projects.Redis_ApiService>("api")
       .WithReference(redis);

builder.Build().Run();
