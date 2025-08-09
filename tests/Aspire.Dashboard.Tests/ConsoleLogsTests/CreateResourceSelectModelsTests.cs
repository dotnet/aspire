// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using Aspire.Dashboard.Model;
using Aspire.Dashboard.Model.Otlp;
using Aspire.Dashboard.Otlp.Model;
using Aspire.Tests.Shared.DashboardModel;
using Xunit;

namespace Aspire.Dashboard.Tests.ConsoleLogsTests;

public class CreateResourceSelectModelsTests
{
    [Fact]
    public void GetViewModels_OneResource_OptionToSelectNotNull()
    {
        // Arrange
        var resources = new List<ResourceViewModel>
        {
            ModelTestHelpers.CreateResource(resourceName: "App1", state: KnownResourceState.Running, displayName: "App1")
        };

        var resourcesByName = new ConcurrentDictionary<string, ResourceViewModel>(resources.ToDictionary(app => app.Name));

        var unknownStateText = "unknown-state";
        var selectAResourceText = "select-a-resource";
        var noSelectionViewModel = new SelectViewModel<ResourceTypeDetails> { Id = null, Name = selectAResourceText };

        // Act
        var viewModels = Components.Pages.ConsoleLogs.GetConsoleLogResourceSelectViewModels(resourcesByName, noSelectionViewModel, unknownStateText, false, out var optionToSelect);

        // Assert
        Assert.NotNull(optionToSelect);

        Assert.Collection(viewModels,
            entry =>
            {
                Assert.NotNull(entry.Id);
                Assert.Equal(OtlpResourceType.Singleton, entry.Id.Type);
                Assert.Equal("App1", entry.Id.InstanceId);

                Assert.Equal("App1", entry.Name);
            });
    }

    [Fact]
    public void GetViewModels_ReturnsRightReplicas()
    {
        // Arrange
        var resources = new List<ResourceViewModel>
        {
            // replica set
            ModelTestHelpers.CreateResource(resourceName: "App1-r1", state: KnownResourceState.Running, displayName: "App1"),
            ModelTestHelpers.CreateResource(resourceName: "App1-r2", displayName: "App1"),

            // singleton, starting state (should be listed in text)
            ModelTestHelpers.CreateResource(resourceName: "App2", state: KnownResourceState.Starting),

            // singleton, finished state (should be listed in text)
            ModelTestHelpers.CreateResource(resourceName: "App3", state: KnownResourceState.Finished),

            // singleton, should not have state in text
            ModelTestHelpers.CreateResource(resourceName: "App4", state: KnownResourceState.Running)
        };

        var resourcesByName = new ConcurrentDictionary<string, ResourceViewModel>(resources.ToDictionary(app => app.Name));

        var unknownStateText = "unknown-state";
        var selectAResourceText = "select-a-resource";
        var noSelectionViewModel = new SelectViewModel<ResourceTypeDetails> { Id = null, Name = selectAResourceText };

        // Act
        var viewModels = Components.Pages.ConsoleLogs.GetConsoleLogResourceSelectViewModels(resourcesByName, noSelectionViewModel, unknownStateText, false, out var optionToSelect);

        // Assert

        Assert.Null(optionToSelect);

        Assert.Collection(viewModels,
            entry =>
            {
                Assert.Equal(entry, noSelectionViewModel);
            },
            entry =>
            {
                Assert.NotNull(entry.Id);
                Assert.Equal(OtlpResourceType.ResourceGrouping, entry.Id.Type);
                Assert.Null(entry.Id.InstanceId);
                Assert.Equal("App1", entry.Id.ReplicaSetName);

                Assert.Equal("App1", entry.Name);
            },
            entry =>
            {
                Assert.NotNull(entry.Id);
                Assert.Equal(OtlpResourceType.Instance, entry.Id.Type);
                Assert.Equal("App1-r1", entry.Id.InstanceId);
                Assert.Equal("App1", entry.Id.ReplicaSetName);

                Assert.Equal("App1-r1", entry.Name);
            },
            entry =>
            {
                Assert.NotNull(entry.Id);
                Assert.Equal(OtlpResourceType.Instance, entry.Id.Type);
                Assert.Equal("App1-r2", entry.Id.InstanceId);
                Assert.Equal("App1", entry.Id.ReplicaSetName);

                Assert.Equal($"App1-r2 ({unknownStateText})", entry.Name);
            },
            entry =>
            {
                Assert.NotNull(entry.Id);
                Assert.Equal(OtlpResourceType.Singleton, entry.Id.Type);
                Assert.Equal("App2", entry.Id.InstanceId);

                Assert.Equal("App2 (Starting)", entry.Name);
            },
            entry =>
            {
                Assert.NotNull(entry.Id);
                Assert.Equal(OtlpResourceType.Singleton, entry.Id.Type);
                Assert.Equal("App3", entry.Id.InstanceId);

                Assert.Equal("App3 (Finished)", entry.Name);
            },
            entry =>
            {
                Assert.NotNull(entry.Id);
                Assert.Equal(OtlpResourceType.Singleton, entry.Id.Type);
                Assert.Equal("App4", entry.Id.InstanceId);

                Assert.Equal("App4", entry.Name);
            });
    }
}
