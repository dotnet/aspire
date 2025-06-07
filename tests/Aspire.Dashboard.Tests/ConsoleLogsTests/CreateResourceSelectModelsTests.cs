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
    public void GetViewModels_ReturnsRightReplicas()
    {
        // Arrange
        var applications = new List<ResourceViewModel>
        {
            // replica set
            ModelTestHelpers.CreateResource(appName: "App1-r1", state: KnownResourceState.Running, displayName: "App1"),
            ModelTestHelpers.CreateResource(appName: "App1-r2", displayName: "App1"),

            // singleton, starting state (should be listed in text)
            ModelTestHelpers.CreateResource(appName: "App2", state: KnownResourceState.Starting),

            // singleton, finished state (should be listed in text)
            ModelTestHelpers.CreateResource(appName: "App3", state: KnownResourceState.Finished),

            // singleton, should not have state in text
            ModelTestHelpers.CreateResource(appName: "App4", state: KnownResourceState.Running)
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

    [Fact]
    public void GetViewModels_SingleResource_KeepsNoneFirstButMakesSingleResourceSelectable()
    {
        // Arrange
        var applications = new List<ResourceViewModel>
        {
            // Single resource - "[none]" should still be first, but the logic elsewhere should auto-select this resource
            ModelTestHelpers.CreateResource(appName: "SingleApp", state: KnownResourceState.Running)
        };

        var resourcesByName = new ConcurrentDictionary<string, ResourceViewModel>(applications.ToDictionary(app => app.Name));

        var unknownStateText = "unknown-state";
        var selectAResourceText = "select-a-resource";
        var noSelectionViewModel = new SelectViewModel<ResourceTypeDetails> { Id = null, Name = selectAResourceText };

        // Act
        var viewModels = Components.Pages.ConsoleLogs.GetConsoleLogResourceSelectViewModels(resourcesByName, noSelectionViewModel, unknownStateText);

        // Assert
        Assert.Equal(2, viewModels.Count);
        
        // "[none]" should always be first
        Assert.Equal(noSelectionViewModel, viewModels[0]);
        
        // The single resource should come second
        Assert.NotNull(viewModels[1].Id);
        Assert.Equal(OtlpApplicationType.Singleton, viewModels[1].Id!.Type);
        Assert.Equal("SingleApp", viewModels[1].Id!.InstanceId);
        Assert.Equal("SingleApp", viewModels[1].Name);
    }
}
