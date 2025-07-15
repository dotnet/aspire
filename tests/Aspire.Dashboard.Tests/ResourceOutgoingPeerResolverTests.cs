// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Aspire.Dashboard.Model;
using Aspire.DashboardService.Proto.V1;
using Aspire.Tests.Shared.DashboardModel;
using Microsoft.AspNetCore.InternalTesting;
using Xunit;
using Value = Google.Protobuf.WellKnownTypes.Value;

namespace Aspire.Dashboard.Tests;

public class ResourceOutgoingPeerResolverTests
{
    private static ResourceViewModel CreateResource(string name, string? serviceAddress = null, int? servicePort = null, string? displayName = null, KnownResourceState? state = null)
    {
        return ModelTestHelpers.CreateResource(
            appName: name,
            displayName: displayName,
            state: state,
            urls: serviceAddress is null || servicePort is null ? [] : [new UrlViewModel(name, new($"http://{serviceAddress}:{servicePort}"), isInternal: false, isInactive: false, displayProperties: UrlDisplayPropertiesViewModel.Empty)]);
    }

    [Fact]
    public void EmptyAttributes_NoMatch()
    {
        // Arrange
        var resources = new Dictionary<string, ResourceViewModel>
        {
            ["test"] = CreateResource("test", "localhost", 5000)
        };

        // Act & Assert
        Assert.False(TryResolvePeerName(resources, [], out _));
    }

    [Fact]
    public void EmptyUrlAttribute_NoMatch()
    {
        // Arrange
        var resources = new Dictionary<string, ResourceViewModel>
        {
            ["test"] = CreateResource("test", "localhost", 5000)
        };

        // Act & Assert
        Assert.False(TryResolvePeerName(resources, [KeyValuePair.Create("peer.service", "")], out _));
    }

    [Fact]
    public void NullUrlAttribute_NoMatch()
    {
        // Arrange
        var resources = new Dictionary<string, ResourceViewModel>
        {
            ["test"] = CreateResource("test", "localhost", 5000)
        };

        // Act & Assert
        Assert.False(TryResolvePeerName(resources, [KeyValuePair.Create<string, string>("peer.service", null!)], out _));
    }

    [Fact]
    public void ExactValueAttribute_Match()
    {
        // Arrange
        var resources = new Dictionary<string, ResourceViewModel>
        {
            ["test"] = CreateResource("test", "localhost", 5000)
        };

        // Act & Assert
        Assert.True(TryResolvePeerName(resources, [KeyValuePair.Create("peer.service", "localhost:5000")], out var value));
        Assert.Equal("test", value);
    }

    [Fact]
    public void NumberAddressValueAttribute_Match()
    {
        // Arrange
        var resources = new Dictionary<string, ResourceViewModel>
        {
            ["test"] = CreateResource("test", "localhost", 5000)
        };

        // Act & Assert
        Assert.True(TryResolvePeerName(resources, [KeyValuePair.Create("peer.service", "127.0.0.1:5000")], out var value));
        Assert.Equal("test", value);
    }

    [Fact]
    public void CommaAddressValueAttribute_Match()
    {
        // Arrange
        var resources = new Dictionary<string, ResourceViewModel>
        {
            ["test"] = CreateResource("test", "localhost", 5000)
        };

        // Act & Assert
        Assert.True(TryResolvePeerName(resources, [KeyValuePair.Create("peer.service", "127.0.0.1,5000")], out var value));
        Assert.Equal("test", value);
    }

    [Fact]
    public void ServerAddressAndPort_Match()
    {
        // Arrange
        var resources = new Dictionary<string, ResourceViewModel>
        {
            ["test"] = CreateResource("test", "localhost", 5000)
        };

        // Act & Assert
        Assert.True(TryResolvePeerName(resources, [KeyValuePair.Create("server.address", "localhost"), KeyValuePair.Create("server.port", "5000")], out var value));
        Assert.Equal("test", value);
    }

    [Fact]
    public async Task OnPeerChanges_DataUpdates_EventRaised()
    {
        // Arrange
        var tcs = new TaskCompletionSource<ResourceViewModelSubscription>(TaskCreationOptions.RunContinuationsAsynchronously);
        var sourceChannel = Channel.CreateUnbounded<ResourceViewModelChange>();
        var resultChannel = Channel.CreateUnbounded<int>();
        var dashboardClient = new MockDashboardClient(tcs.Task);
        var resolver = new ResourceOutgoingPeerResolver(dashboardClient);
        var changeCount = 0;
        resolver.OnPeerChanges(async () =>
        {
            await resultChannel.Writer.WriteAsync(++changeCount);
        });

        var readValue = 0;
        Assert.False(resultChannel.Reader.TryRead(out readValue));

        // Act 1
        // Initial resource causes change.
        tcs.SetResult(new ResourceViewModelSubscription(
            [CreateResource("test", serviceAddress: "localhost", servicePort: 8080)],
            GetChanges()));

        // Assert 1
        readValue = await resultChannel.Reader.ReadAsync().DefaultTimeout();
        Assert.Equal(1, readValue);

        // Act 2
        // New resource causes change.
        await sourceChannel.Writer.WriteAsync(new ResourceViewModelChange(ResourceViewModelChangeType.Upsert, CreateResource("test2", serviceAddress: "localhost", servicePort: 8080, state: KnownResourceState.Starting)));

        // Assert 2
        readValue = await resultChannel.Reader.ReadAsync().DefaultTimeout();
        Assert.Equal(2, readValue);

        // Act 3
        // URL change causes change.
        await sourceChannel.Writer.WriteAsync(new ResourceViewModelChange(ResourceViewModelChangeType.Upsert, CreateResource("test2", serviceAddress: "localhost", servicePort: 8081, state: KnownResourceState.Starting)));

        // Assert 3
        readValue = await resultChannel.Reader.ReadAsync().DefaultTimeout();
        Assert.Equal(3, readValue);

        // Act 4
        // Resource update doesn't cause change.
        await sourceChannel.Writer.WriteAsync(new ResourceViewModelChange(ResourceViewModelChangeType.Upsert, CreateResource("test2", serviceAddress: "localhost", servicePort: 8081, state: KnownResourceState.Running)));

        // Dispose so that we know that all changes are processed.
        await resolver.DisposeAsync().DefaultTimeout();
        resultChannel.Writer.Complete();

        // Assert 4
        Assert.False(await resultChannel.Reader.WaitToReadAsync().DefaultTimeout());
        Assert.Equal(3, changeCount);

        async IAsyncEnumerable<IReadOnlyList<ResourceViewModelChange>> GetChanges([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await foreach (var item in sourceChannel.Reader.ReadAllAsync(cancellationToken))
            {
                yield return [item];
            }
        }
    }

    [Fact]
    public void NameAndDisplayNameDifferent_OneInstance_ReturnDisplayName()
    {
        // Arrange
        var resources = new Dictionary<string, ResourceViewModel>
        {
            ["test-abc"] = CreateResource("test-abc", "localhost", 5000, displayName: "test")
        };

        // Act & Assert
        Assert.True(TryResolvePeerName(resources, [KeyValuePair.Create("server.address", "localhost"), KeyValuePair.Create("server.port", "5000")], out var value));
        Assert.Equal("test", value);
    }

    [Fact]
    public void NameAndDisplayNameDifferent_MultipleInstances_ReturnName()
    {
        // Arrange
        var resources = new Dictionary<string, ResourceViewModel>
        {
            ["test-abc"] = CreateResource("test-abc", "localhost", 5000, displayName: "test"),
            ["test-def"] = CreateResource("test-def", "localhost", 5001, displayName: "test")
        };

        // Act & Assert
        Assert.True(TryResolvePeerName(resources, [KeyValuePair.Create("server.address", "localhost"), KeyValuePair.Create("server.port", "5000")], out var value1));
        Assert.Equal("test-abc", value1);

        Assert.True(TryResolvePeerName(resources, [KeyValuePair.Create("server.address", "localhost"), KeyValuePair.Create("server.port", "5001")], out var value2));
        Assert.Equal("test-def", value2);
    }

    private static bool TryResolvePeerName(IDictionary<string, ResourceViewModel> resources, KeyValuePair<string, string>[] attributes, out string? peerName)
    {
        return ResourceOutgoingPeerResolver.TryResolvePeerNameCore(resources, attributes, out peerName, out _);
    }

    [Fact]
    public void ConnectionStringWithEndpoint_Match()
    {
        // Arrange - GitHub Models resource with connection string containing endpoint
        var connectionString = "Endpoint=https://models.github.ai/inference;Key=test-key;Model=openai/gpt-4o-mini;DeploymentId=openai/gpt-4o-mini";
        var resources = new Dictionary<string, ResourceViewModel>
        {
            ["github-model"] = CreateResourceWithConnectionString("github-model", connectionString)
        };

        // Act & Assert
        Assert.True(TryResolvePeerName(resources, [KeyValuePair.Create("peer.service", "models.github.ai:443")], out var value));
        Assert.Equal("github-model", value);
    }

    [Fact]
    public void ConnectionStringWithEndpointOrganization_Match()
    {
        // Arrange - GitHub Models resource with organization endpoint
        var connectionString = "Endpoint=https://models.github.ai/orgs/myorg/inference;Key=test-key;Model=openai/gpt-4o-mini;DeploymentId=openai/gpt-4o-mini";
        var resources = new Dictionary<string, ResourceViewModel>
        {
            ["github-model"] = CreateResourceWithConnectionString("github-model", connectionString)
        };

        // Act & Assert
        Assert.True(TryResolvePeerName(resources, [KeyValuePair.Create("peer.service", "models.github.ai:443")], out var value));
        Assert.Equal("github-model", value);
    }

    [Fact]
    public void ParameterWithUrlValue_Match()
    {
        // Arrange - Parameter resource with URL value
        var resources = new Dictionary<string, ResourceViewModel>
        {
            ["api-url-param"] = CreateResourceWithParameterValue("api-url-param", "https://api.example.com:8080/endpoint")
        };

        // Act & Assert
        Assert.True(TryResolvePeerName(resources, [KeyValuePair.Create("peer.service", "api.example.com:8080")], out var value));
        Assert.Equal("api-url-param", value);
    }

    [Fact]
    public void ConnectionStringWithoutEndpoint_NoMatch()
    {
        // Arrange - Connection string without Endpoint property
        var connectionString = "Server=localhost;Database=test;User=admin;Password=secret";
        var resources = new Dictionary<string, ResourceViewModel>
        {
            ["sql-connection"] = CreateResourceWithConnectionString("sql-connection", connectionString)
        };

        // Act & Assert
        Assert.False(TryResolvePeerName(resources, [KeyValuePair.Create("peer.service", "localhost:1433")], out _));
    }

    [Fact]
    public void ParameterWithNonUrlValue_NoMatch()
    {
        // Arrange - Parameter resource with non-URL value
        var resources = new Dictionary<string, ResourceViewModel>
        {
            ["config-param"] = CreateResourceWithParameterValue("config-param", "simple-config-value")
        };

        // Act & Assert
        Assert.False(TryResolvePeerName(resources, [KeyValuePair.Create("peer.service", "localhost:5000")], out _));
    }

    [Fact]
    public void ConnectionStringAsDirectUrl_Match()
    {
        // Arrange - Connection string that is itself a URL (e.g., blob storage)
        var connectionString = "https://mystorageaccount.blob.core.windows.net/";
        var resources = new Dictionary<string, ResourceViewModel>
        {
            ["blob-storage"] = CreateResourceWithConnectionString("blob-storage", connectionString)
        };

        // Act & Assert
        Assert.True(TryResolvePeerName(resources, [KeyValuePair.Create("peer.service", "mystorageaccount.blob.core.windows.net:443")], out var value));
        Assert.Equal("blob-storage", value);
    }

    [Fact]
    public void ConnectionStringAsDirectUrlWithCustomPort_Match()
    {
        // Arrange - Connection string that is itself a URL with custom port
        var connectionString = "https://myvault.vault.azure.net:8080/";
        var resources = new Dictionary<string, ResourceViewModel>
        {
            ["key-vault"] = CreateResourceWithConnectionString("key-vault", connectionString)
        };

        // Act & Assert
        Assert.True(TryResolvePeerName(resources, [KeyValuePair.Create("peer.service", "myvault.vault.azure.net:8080")], out var value));
        Assert.Equal("key-vault", value);
    }

    private static ResourceViewModel CreateResourceWithConnectionString(string name, string connectionString)
    {
        var properties = new Dictionary<string, ResourcePropertyViewModel>
        {
            [KnownProperties.Resource.ConnectionString] = new(
                name: KnownProperties.Resource.ConnectionString,
                value: Value.ForString(connectionString),
                isValueSensitive: false,
                knownProperty: null,
                priority: 0)
        };

        return ModelTestHelpers.CreateResource(
            appName: name,
            resourceType: KnownResourceTypes.ConnectionString,
            properties: properties);
    }

    private static ResourceViewModel CreateResourceWithParameterValue(string name, string value)
    {
        var properties = new Dictionary<string, ResourcePropertyViewModel>
        {
            [KnownProperties.Parameter.Value] = new(
                name: KnownProperties.Parameter.Value,
                value: Value.ForString(value),
                isValueSensitive: false,
                knownProperty: null,
                priority: 0)
        };

        return ModelTestHelpers.CreateResource(
            appName: name,
            resourceType: KnownResourceTypes.Parameter,
            properties: properties);
    }

    [Fact]
    public void MultipleResourcesMatch_SqlServerAddresses_ReturnsFalse()
    {
        // Arrange - Multiple SQL Server resources with same address
        var resources = new Dictionary<string, ResourceViewModel>
        {
            ["sqlserver1"] = CreateResource("sqlserver1", "localhost", 1433),
            ["sqlserver2"] = CreateResource("sqlserver2", "localhost", 1433)
        };

        // Act & Assert - Both resources would match "localhost:1433"
        // so this should return false (ambiguous match)
        Assert.False(TryResolvePeerName(resources, [KeyValuePair.Create("peer.service", "localhost:1433")], out var name));
        Assert.Null(name);
    }

    [Fact]
    public void MultipleResourcesMatch_RedisAddresses_ReturnsFalse()
    {
        // Arrange - Multiple Redis resources with equivalent addresses  
        var resources = new Dictionary<string, ResourceViewModel>
        {
            ["redis-cache"] = CreateResource("redis-cache", "localhost", 6379),
            ["redis-session"] = CreateResource("redis-session", "localhost", 6379)
        };

        // Act & Assert - Both resources would match "localhost:6379" 
        // so this should return false (ambiguous match)
        Assert.False(TryResolvePeerName(resources, [KeyValuePair.Create("peer.service", "localhost:6379")], out var name));
        Assert.Null(name);
    }

    [Fact]
    public void MultipleResourcesMatch_SqlServerCommaFormat_ReturnsFalse()
    {
        // Arrange - Multiple SQL Server resources where comma format would match both
        var resources = new Dictionary<string, ResourceViewModel>
        {
            ["sqldb1"] = CreateResource("sqldb1", "localhost", 1433),
            ["sqldb2"] = CreateResource("sqldb2", "localhost", 1433)
        };

        // Act & Assert - SQL Server comma format "localhost,1433" should match both resources
        // so this should return false (ambiguous match)
        Assert.False(TryResolvePeerName(resources, [KeyValuePair.Create("peer.service", "localhost,1433")], out var name));
        Assert.Null(name);
    }

    [Fact]  
    public void MultipleResourcesMatch_MixedPortFormats_ReturnsFalse()
    {
        // Arrange - Resources with same logical address but different port formats
        var resources = new Dictionary<string, ResourceViewModel>
        {
            ["db-primary"] = CreateResource("db-primary", "dbserver", 5432),
            ["db-replica"] = CreateResource("db-replica", "dbserver", 5432)
        };

        // Act & Assert - Should be ambiguous since both resources have same address
        Assert.False(TryResolvePeerName(resources, [KeyValuePair.Create("server.address", "dbserver"), KeyValuePair.Create("server.port", "5432")], out var name));
        Assert.Null(name);
    }

    [Fact]
    public void MultipleResourcesMatch_AddressTransformation_ReturnsFalse()
    {
        // Arrange - Multiple resources with exact same address (not just after transformation)
        var resources = new Dictionary<string, ResourceViewModel>
        {
            ["web-frontend"] = CreateResource("web-frontend", "localhost", 8080),
            ["web-backend"] = CreateResource("web-backend", "localhost", 8080)
        };

        // Act & Assert - Both resources have identical cached address "localhost:8080"
        // so this should return false (ambiguous match)
        Assert.False(TryResolvePeerName(resources, [KeyValuePair.Create("peer.service", "localhost:8080")], out var name));
        Assert.Null(name);
    }

    [Fact]
    public void MultipleResourcesMatch_ViaTransformation_ReturnsFirstMatch()
    {
        // Arrange - Resources that become ambiguous after address transformation
        // Note: This test documents current behavior where transformation order matters
        var resources = new Dictionary<string, ResourceViewModel>
        {
            ["sql-primary"] = CreateResource("sql-primary", "localhost", 1433),
            ["sql-replica"] = CreateResource("sql-replica", "127.0.0.1", 1433)
        };

        // Act & Assert - Due to transformation order, this currently finds sql-replica first
        // before the transformation that would make sql-primary match as well
        Assert.True(TryResolvePeerName(resources, [KeyValuePair.Create("peer.service", "127.0.0.1:1433")], out var name));
        Assert.Equal("sql-replica", name);
    }

    [Fact]
    public void SingleResourceAfterTransformation_ReturnsTrue()
    {
        // Arrange - Only one resource that matches after address transformation
        var resources = new Dictionary<string, ResourceViewModel>
        {
            ["unique-service"] = CreateResource("unique-service", "localhost", 8080),
            ["other-service"] = CreateResource("other-service", "remotehost", 9090)
        };

        // Act & Assert - Only the first resource should match "127.0.0.1:8080" after transformation
        Assert.True(TryResolvePeerName(resources, [KeyValuePair.Create("peer.service", "127.0.0.1:8080")], out var name));
        Assert.Equal("unique-service", name);
    }

    private sealed class MockDashboardClient(Task<ResourceViewModelSubscription> subscribeResult) : IDashboardClient
    {
        public bool IsEnabled => true;
        public Task WhenConnected => Task.CompletedTask;
        public string ApplicationName => "ApplicationName";
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        public Task<ResourceCommandResponseViewModel> ExecuteResourceCommandAsync(string resourceName, string resourceType, CommandViewModel command, CancellationToken cancellationToken) => throw new NotImplementedException();
        public IAsyncEnumerable<IReadOnlyList<ResourceLogLine>> GetConsoleLogs(string resourceName, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task SendInteractionRequestAsync(WatchInteractionsRequestUpdate request, CancellationToken cancellationToken) => throw new NotImplementedException();
        public IAsyncEnumerable<IReadOnlyList<ResourceLogLine>> SubscribeConsoleLogs(string resourceName, CancellationToken cancellationToken) => throw new NotImplementedException();
        public IAsyncEnumerable<WatchInteractionsResponseUpdate> SubscribeInteractionsAsync(CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<ResourceViewModelSubscription> SubscribeResourcesAsync(CancellationToken cancellationToken) => subscribeResult;
    }
}
