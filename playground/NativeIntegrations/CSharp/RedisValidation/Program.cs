// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Native.Authoring;
using Aspire.Hosting.Native.Client;
using NativeIntegrations.Redis;
using NativeIntegrations.Redis.AppHost;

var initial = Compose("write", "real-authoring-password");
await using var application = initial.Builder.Build();
await application.StartAsync();
await application.WaitForCompletionAsync(initial.Consumer);
var rejected = Compose("read", "real-authoring-password");
rejected.Builder.AddExecutable("invalid", "not-an-absolute-executable", ".");
try
{
    await application.UpdateAsync(rejected.Builder);
    throw new InvalidOperationException("Invalid native declarations must not succeed.");
}
catch (NativeCapabilityException exception) when (exception.Code == "INVALID_ARGUMENT")
{
}
await application.WaitForCompletionAsync(initial.Consumer);
var next = Compose("read", "real-authoring-password-rotated");
await application.UpdateAsync(next.Builder);
await application.WaitForCompletionAsync(next.Consumer);
await application.DisposeAsync();
Console.WriteLine("redis-persistence-ok");

(DistributedApplicationBuilder Builder, IResourceBuilder<ExecutableResource> Consumer) Compose(string operation, string password)
{
    var builder = DistributedApplication.CreateBuilder(args);
    var credential = builder.AddParameter("cache-password", password);
    var cache = builder.AddRedis("cache", credential).WithDataVolume(builder.Configuration["data-volume"]);
    var consumer = builder.AddExecutable("consumer", builder.Configuration["consumer-path"], AppContext.BaseDirectory)
        .WithArgs("--redis-consume", operation)
        .WithReference(cache)
        .WaitFor(cache);

    return (builder, consumer);
}
