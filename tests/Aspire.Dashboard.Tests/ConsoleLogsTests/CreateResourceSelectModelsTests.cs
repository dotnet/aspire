// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Collections.Frozen;
using Aspire.Dashboard.Model;
using Aspire.Dashboard.Model.Otlp;
using Aspire.Dashboard.Otlp.Model;
using Google.Protobuf.WellKnownTypes;
using Xunit;

namespace Aspire.Dashboard.Tests.ConsoleLogsTests;

public class CreateResourceSelectModelsTests
{
    [Fact]
    public void GetViewModels_ReturnsRightReplicas()
    {
        // Arrange
        var applications = new List<ResourceViewModel>
        {
            // replica set
            CreateResourceViewModel("App1-r1", KnownResourceState.Running, displayName: "App1"),
            CreateResourceViewModel("App1-r2", null, displayName: "App1"),

            // singleton, starting state (should be listed in text)
            CreateResourceViewModel("App2", KnownResourceState.Starting),

            // singleton, finished state (should be listed in text)
            CreateResourceViewModel("App3", KnownResourceState.Finished),

            // singleton, should not have state in text
            CreateResourceViewModel("App4", KnownResourceState.Running)
        };

        var resourcesByName = new ConcurrentDictionary<string, ResourceViewModel>(applications.ToDictionary(app => app.Name));

        var unknownStateText = "unknown-state";
        var selectAResourceText = "select-a-resource";
        var noSelectionViewModel = new SelectViewModel<ResourceTypeDetails> { Id = null, Name = selectAResourceText };

        // Act
        var viewModels = Components.Pages.ConsoleLogs.GetConsoleLogResourceSelectViewModels(resourcesByName, noSelectionViewModel, unknownStateText);

        // Assert
        Assert.Collection(viewModels,
            entry =>
            {
                Assert.Equal(entry, noSelectionViewModel);
            },
            entry =>
            {
                Assert.NotNull(entry.Id);
                Assert.Equal(OtlpApplicationType.ResourceGrouping, entry.Id.Type);
                Assert.Null(entry.Id.InstanceId);
                Assert.Equal("App1", entry.Id.ReplicaSetName);

                Assert.Equal("App1", entry.Name);
            },
            entry =>
            {
                Assert.NotNull(entry.Id);
                Assert.Equal(OtlpApplicationType.Instance, entry.Id.Type);
                Assert.Equal("App1-r1", entry.Id.InstanceId);
                Assert.Equal("App1", entry.Id.ReplicaSetName);

                Assert.Equal("App1-r1", entry.Name);
            },
            entry =>
            {
                Assert.NotNull(entry.Id);
                Assert.Equal(OtlpApplicationType.Instance, entry.Id.Type);
                Assert.Equal("App1-r2", entry.Id.InstanceId);
                Assert.Equal("App1", entry.Id.ReplicaSetName);

                Assert.Equal($"App1-r2 ({unknownStateText})", entry.Name);
            },
            entry =>
            {
                Assert.NotNull(entry.Id);
                Assert.Equal(OtlpApplicationType.Singleton, entry.Id.Type);
                Assert.Equal("App2", entry.Id.InstanceId);

                Assert.Equal("App2 (Starting)", entry.Name);
            },
            entry =>
            {
                Assert.NotNull(entry.Id);
                Assert.Equal(OtlpApplicationType.Singleton, entry.Id.Type);
                Assert.Equal("App3", entry.Id.InstanceId);

                Assert.Equal("App3 (Finished)", entry.Name);
            },
            entry =>
            {
                Assert.NotNull(entry.Id);
                Assert.Equal(OtlpApplicationType.Singleton, entry.Id.Type);
                Assert.Equal("App4", entry.Id.InstanceId);

                Assert.Equal("App4", entry.Name);
            });
    }

    // display name will be replica set when there are multiple resources with the same display name
    private static ResourceViewModel CreateResourceViewModel(string appName, KnownResourceState? state, string? displayName = null)
    {
        return new ResourceViewModel
        {
            Name = appName,
            ResourceType = "CustomResource",
            DisplayName = displayName ?? appName,
            Uid = Guid.NewGuid().ToString(),
            CreationTimeStamp = DateTime.UtcNow,
            Environment = [],
            Properties = FrozenDictionary<string, Value>.Empty,
            Urls = [],
            Volumes = [],
            State = state?.ToString(),
            KnownState = state,
            StateStyle = null,
            Commands = []
        };
    }
}
