// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Frozen;
using System.Collections.Immutable;
using Aspire.Dashboard.Model;
using Google.Protobuf.WellKnownTypes;

namespace Aspire.Dashboard.Tests.Integration.Playwright;

public sealed class MockDashboardClient : IDashboardClient
{
    public static readonly ResourceViewModel TestResource1 = new()
    {
        Name = "TestResource",
        DisplayName = "TestResource",
        Commands = ImmutableArray<CommandViewModel>.Empty,
        CreationTimeStamp = DateTime.Now,
        Environment = ImmutableArray<EnvironmentVariableViewModel>.Empty,
        ResourceType = KnownResourceTypes.Project,
        Properties = new []
        {
            new KeyValuePair<string, Value>(KnownProperties.Project.Path, new Value()
            {
                StringValue = "C:/MyProjectPath/Project.csproj"
            })
        }.ToFrozenDictionary(),
        State = "Running",
        Uid = Guid.NewGuid().ToString(),
        StateStyle = null,
        Urls = ImmutableArray<UrlViewModel>.Empty,

    };

    public bool IsEnabled => true;
    public Task WhenConnected => Task.CompletedTask;
    public string ApplicationName => "IntegrationTestApplication";
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    public Task<ResourceCommandResponseViewModel> ExecuteResourceCommandAsync(string resourceName, string resourceType, CommandViewModel command, CancellationToken cancellationToken) => throw new NotImplementedException();
    public IAsyncEnumerable<IReadOnlyList<ResourceLogLine>>? SubscribeConsoleLogs(string resourceName, CancellationToken cancellationToken) => throw new NotImplementedException();

    public Task<ResourceViewModelSubscription> SubscribeResourcesAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(new ResourceViewModelSubscription(
            [TestResource1],
            Test()
        ));
    }

    private static async IAsyncEnumerable<IReadOnlyList<ResourceViewModelChange>> Test()
    {
        await Task.CompletedTask;
        yield return new List<ResourceViewModelChange> { };
    }
}
