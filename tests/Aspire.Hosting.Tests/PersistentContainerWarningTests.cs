// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREUSERSECRETS001

using Aspire.Hosting.UserSecrets;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;

namespace Aspire.Hosting.Tests;

public class PersistentContainerWarningTests
{
    [Fact]
    public async Task PersistentContainerWithoutUserSecrets_LogsWarning()
    {
        var testSink = new TestSink();

        var services = new ServiceCollection();
        services.AddSingleton<IUserSecretsManager>(NoopUserSecretsManager.Instance);
        services.AddLogging(logging => logging.AddProvider(new TestLoggerProvider(testSink)));
        var serviceProvider = services.BuildServiceProvider();

        var resources = new ResourceCollection();
        var container = new ContainerResource("my-container");
        container.Annotations.Add(new ContainerLifetimeAnnotation { Lifetime = ContainerLifetime.Persistent });
        resources.Add(container);

        var model = new DistributedApplicationModel(resources);
        var beforeStartEvent = new BeforeStartEvent(serviceProvider, model);

        await BuiltInDistributedApplicationEventSubscriptionHandlers.WarnPersistentContainersWithoutUserSecrets(beforeStartEvent, CancellationToken.None);

        Assert.Contains(testSink.Writes, w => w.LogLevel == LogLevel.Warning && w.Message?.Contains("my-container") == true);
    }

    [Fact]
    public async Task PersistentContainerWithUserSecrets_DoesNotLogWarning()
    {
        var testSink = new TestSink();

        var services = new ServiceCollection();
        services.AddSingleton<IUserSecretsManager>(new FakeAvailableUserSecretsManager());
        services.AddLogging(logging => logging.AddProvider(new TestLoggerProvider(testSink)));
        var serviceProvider = services.BuildServiceProvider();

        var resources = new ResourceCollection();
        var container = new ContainerResource("my-container");
        container.Annotations.Add(new ContainerLifetimeAnnotation { Lifetime = ContainerLifetime.Persistent });
        resources.Add(container);

        var model = new DistributedApplicationModel(resources);
        var beforeStartEvent = new BeforeStartEvent(serviceProvider, model);

        await BuiltInDistributedApplicationEventSubscriptionHandlers.WarnPersistentContainersWithoutUserSecrets(beforeStartEvent, CancellationToken.None);

        Assert.DoesNotContain(testSink.Writes, w => w.LogLevel == LogLevel.Warning);
    }

    [Fact]
    public async Task SessionContainerWithoutUserSecrets_DoesNotLogWarning()
    {
        var testSink = new TestSink();

        var services = new ServiceCollection();
        services.AddSingleton<IUserSecretsManager>(NoopUserSecretsManager.Instance);
        services.AddLogging(logging => logging.AddProvider(new TestLoggerProvider(testSink)));
        var serviceProvider = services.BuildServiceProvider();

        var resources = new ResourceCollection();
        resources.Add(new ContainerResource("my-container"));

        var model = new DistributedApplicationModel(resources);
        var beforeStartEvent = new BeforeStartEvent(serviceProvider, model);

        await BuiltInDistributedApplicationEventSubscriptionHandlers.WarnPersistentContainersWithoutUserSecrets(beforeStartEvent, CancellationToken.None);

        Assert.DoesNotContain(testSink.Writes, w => w.LogLevel == LogLevel.Warning);
    }

    private sealed class FakeAvailableUserSecretsManager : IUserSecretsManager
    {
        public bool IsAvailable => true;
        public string FilePath => string.Empty;
        public bool TrySetSecret(string name, string value) => true;
        public void GetOrSetSecret(Microsoft.Extensions.Configuration.IConfigurationManager configuration, string name, Func<string> valueGenerator) { }
        public Task SaveStateAsync(System.Text.Json.Nodes.JsonObject state, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
