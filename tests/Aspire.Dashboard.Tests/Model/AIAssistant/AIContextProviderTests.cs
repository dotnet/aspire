// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Channels;
using Aspire.Dashboard.Configuration;
using Aspire.Dashboard.Model.Assistant;
using Aspire.Dashboard.Model.Assistant.Prompts;
using Aspire.Dashboard.Resources;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Aspire.Dashboard.Tests.Model.AIAssistant;

public class AIContextProviderTests
{
    [Fact]
    public void Add_Context_IncreasesCount()
    {
        // Arrange
        var provider = CreateAIContextProvider();

        // Act
        var context = provider.AddNew("test", c => { });

        // Assert
        Assert.Equal(1, provider.ProviderCount);
        Assert.Equal(context, provider.GetContext());
    }

    [Fact]
    public void Dispose_Context_DecreasesCount()
    {
        // Arrange
        var provider = CreateAIContextProvider();
        var context = provider.AddNew("test", c => { });

        // Act
        context.Dispose();

        // Assert
        Assert.Equal(0, provider.ProviderCount);
        Assert.Null(provider.GetContext());
    }

    [Fact]
    public void Remove_Context_OutOfOrder_RemovesSuccessfully()
    {
        // Arrange
        var provider = CreateAIContextProvider();

        // Act 1
        var context1 = provider.AddNew("test1", c => { });
        var context2 = provider.AddNew("test2", c => { });

        // Assert 1
        Assert.Equal(context2, provider.GetContext());
        Assert.Equal(2, provider.ProviderCount);

        // Act 2
        context1.Dispose();

        // Assert 2
        Assert.Equal(context2, provider.GetContext());
        Assert.Equal(1, provider.ProviderCount);

        // Act 3
        context2.Dispose();

        // Assert 3
        Assert.Null(provider.GetContext());
        Assert.Equal(0, provider.ProviderCount);
    }

    [Fact]
    public async Task Subscribe_Context_OutOfOrder_RemovesSuccessfully()
    {
        // Arrange
        var changeChannel = Channel.CreateUnbounded<AIContext?>();

        var provider = CreateAIContextProvider();
        var subscription = provider.OnContextChanged(() =>
        {
            changeChannel.Writer.TryWrite(provider.GetContext());
            return Task.CompletedTask;
        });
        Assert.Equal(1, provider.SubscriptionCount);

        // Act & Assert
        var context1 = provider.AddNew("test1", c => { });
        var newContext = await changeChannel.Reader.ReadAsync().DefaultTimeout();
        Assert.Equal(context1, newContext);

        var context2 = provider.AddNew("test2", c => { });
        newContext = await changeChannel.Reader.ReadAsync().DefaultTimeout();
        Assert.Equal(context2, newContext);

        Assert.Equal(context2, provider.GetContext());

        context1.Dispose();
        var newContextTask = changeChannel.Reader.ReadAsync();
        Assert.False(newContextTask.IsCompleted);

        Assert.Equal(context2, provider.GetContext());

        context2.Dispose();
        newContext = await newContextTask.DefaultTimeout();
        Assert.Null(newContext);

        Assert.Null(provider.GetContext());

        changeChannel.Writer.Complete();
        Assert.False(await changeChannel.Reader.WaitToReadAsync().DefaultTimeout());

        subscription.Dispose();
        Assert.Equal(0, provider.SubscriptionCount);
    }

    private static AIContextProvider CreateAIContextProvider()
    {
        var testOptionsMonitor = new TestOptionsMonitor<DashboardOptions>(new DashboardOptions());

        return new AIContextProvider(
            new ServiceCollection().BuildServiceProvider(),
            NullLogger<AIContextProvider>.Instance,
            testOptionsMonitor,
            new ChatClientFactory(new ConfigurationManager(), NullLoggerFactory.Instance, testOptionsMonitor),
            new IceBreakersBuilder(new TestStringLocalizer<AIPrompts>()),
            new TestTelemetryErrorRecorder());
    }
}
