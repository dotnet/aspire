// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Components.Common.Tests;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;
using Xunit.Abstractions;

namespace Aspire.Hosting.Azure.Tests;

public class AzureServiceBusEmulatorFunctionalTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    [RequiresDocker]
    public async Task VerifyAzureServiceBusEmulatorResource()
    {
        var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));

        using var builder = TestDistributedApplicationBuilder.Create().WithTestAndResourceLogging(testOutputHelper);
        var serviceBus = builder.AddAzureServiceBus("sbemul").RunAsEmulator().AddQueue("queue1");

        using var app = builder.Build();
        await app.StartAsync();

        var hb = Host.CreateApplicationBuilder();
        hb.Configuration["ConnectionStrings:ServiceBusConnection"] = await serviceBus.Resource.ConnectionStringExpression.GetValueAsync(CancellationToken.None);
        hb.AddAzureServiceBusClient("ServiceBusConnection");

        using var host = hb.Build();
        await host.StartAsync(cts.Token);

        var rns = app.Services.GetRequiredService<ResourceNotificationService>();
        await rns.WaitForResourceAsync(serviceBus.Resource.Name, KnownResourceStates.Running, cts.Token);

        var serviceBusClient = host.Services.GetRequiredService<ServiceBusClient>();

        await using var sender = serviceBusClient.CreateSender("queue1");
        await sender.SendMessageAsync(new ServiceBusMessage("Hello, World!"), cts.Token);

        await using var receiver = serviceBusClient.CreateReceiver("queue1");
        var message = await receiver.ReceiveMessageAsync(cancellationToken: cts.Token);

        Assert.Equal("Hello, World!", message.Body.ToString());
    }
}
