// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using Aspire.Hosting.Native.Client;
using Microsoft.Extensions.Logging.Abstractions;
using NativeIntegration.Generated;

var token = Environment.GetEnvironmentVariable("ASPIRE_NATIVE_RPC_TOKEN")
    ?? throw new InvalidOperationException("Set ASPIRE_NATIVE_RPC_TOKEN for the native server.");
await using var native = await NativeClient.ConnectAsync(IPEndPoint.Parse(args[0]), token,
    TimeSpan.FromSeconds(15), TimeProvider.System);
await using var provider = await NativeProviderActivation.StartAsync(native, "example.csharp.redis",
    args[1], [], TimeProvider.System, NullLogger.Instance, CancellationToken.None);
var workspace = await native.Server.CreateApplicationWorkspaceAsync();
var started = false;
foreach (var operation in new[] { "write", "read" })
{
    var application = await workspace.BeginRevisionAsync();
    var password = await application.AddSecretParameterAsync("cache-password",
        operation == "write" ? "real-authoring-password" : "real-authoring-password-rotated");
    var cache = await application.AddRedisAsync("cache", password);
    await cache.WithDataVolumeAsync(args[2]);
    var consumer = await application.AddExecutableAsync("consumer", args[1], Path.GetDirectoryName(args[1])!);
    await consumer.WithArgumentAsync(await application.CreateLiteralValueAsync("--redis-consume"));
    await consumer.WithArgumentAsync(await application.CreateLiteralValueAsync(operation));
    await cache.WithReferenceAsync(consumer, "ConnectionStrings__cache");
    await workspace.CommitRevisionAsync(application);
    var running = started ? await workspace.GetApplicationExecutionAsync() : await application.StartApplicationAsync();
    started = true;
    await running.WaitApplicationReadyAsync(60000);
    if (!await running.WaitResourceCompletionAsync(consumer, 60000))
    {
        throw new InvalidOperationException("The real Redis consumer failed.");
    }
}
await workspace.RetireApplicationWorkspaceAsync();
Console.WriteLine("redis-persistence-ok");
