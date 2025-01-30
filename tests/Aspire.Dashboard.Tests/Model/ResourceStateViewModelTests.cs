// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Model;
using Aspire.Dashboard.Resources;
using Aspire.Tests.Shared.DashboardModel;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.FluentUI.AspNetCore.Components;
using Xunit;
using Enum = System.Enum;

namespace Aspire.Dashboard.Tests.Model;

public class ResourceStateViewModelTests
{
    private const string ResourceType = "Container";

    [Theory]
    // Resource is no longer running
    [InlineData(
        /* state */ KnownResourceState.Exited, null, null,null,
        /* expected output */ $"Localized:{nameof(Columns.StateColumnResourceExited)}:{ResourceType}", "Warning", Color.Warning, "Exited")]
    [InlineData(
        /* state */ KnownResourceState.Exited, 3, null, null,
        /* expected output */ $"Localized:{nameof(Columns.StateColumnResourceExitedUnexpectedly)}:{ResourceType}+3", "ErrorCircle", Color.Error, "Exited")]
    [InlineData(
        /* state */ KnownResourceState.Finished, 0, null, null,
        /* expected output */ $"Localized:{nameof(Columns.StateColumnResourceExited)}:{ResourceType}", "RecordStop", Color.Info, "Finished")]
    [InlineData(
        /* state */ KnownResourceState.Unknown, null, null, null,
        /* expected output */ "Unknown", "CircleHint", Color.Info, "Unknown")]
    // Health checks
    [InlineData(
        /* state */ KnownResourceState.Running, null, "Healthy", null,
        /* expected output */ "Running", "CheckmarkCircle", Color.Success, "Running")]
    [InlineData(
        /* state */ KnownResourceState.Running, null, "", null,
        /* expected output */ $"Localized:{nameof(Columns.RunningAndUnhealthyResourceStateToolTip)}", "CheckmarkCircleWarning", Color.Warning, "Running (Unhealthy)")]
    [InlineData(
        /* state */ KnownResourceState.Running, null, "Unhealthy", null,
        /* expected output */ $"Localized:{nameof(Columns.RunningAndUnhealthyResourceStateToolTip)}", "CheckmarkCircleWarning", Color.Warning, "Running (Unhealthy)")]
    [InlineData(
        /* state */ KnownResourceState.Running, null, "Healthy", "warning",
        /* expected output */ "Running", "Warning", Color.Warning, "Running")]
    [InlineData(
        /* state */ KnownResourceState.Running, null, "Healthy", "NOT_A_VALID_STATE_STYLE",
        /* expected output */ "Running", "Circle", Color.Neutral, "Running")]
    [InlineData(
        /* state */ KnownResourceState.RuntimeUnhealthy, null, null, null,
        /* expected output */ $"Localized:{nameof(Columns.StateColumnResourceContainerRuntimeUnhealthy)}", "Warning", Color.Warning, "Runtime unhealthy")]
    public void ResourceViewModel_ReturnsCorrectIconAndTooltip(
        KnownResourceState state,
        int? exitCode,
        string? healthStatusString,
        string? stateStyle,
        string? expectedTooltip,
        string expectedIconName,
        Color expectedColor,
        string expectedText)
    {
        // Arrange
        HealthStatus? healthStatus = string.IsNullOrEmpty(healthStatusString) ? null : Enum.Parse<HealthStatus>(healthStatusString);
        var propertiesDictionary = new Dictionary<string, ResourcePropertyViewModel>();
        if (exitCode is not null)
        {
            propertiesDictionary.TryAdd(KnownProperties.Resource.ExitCode, new ResourcePropertyViewModel(KnownProperties.Resource.ExitCode, Value.ForNumber((double)exitCode), false, null, 0, new BrowserTimeProvider(new NullLoggerFactory())));
        }

        var resource = ModelTestHelpers.CreateResource(
            state: state,
            reportHealthStatus: healthStatus,
            createNullHealthReport: healthStatusString == "",
            stateStyle: stateStyle,
            resourceType: ResourceType,
            properties: propertiesDictionary);

        if (exitCode is not null)
        {
            resource.Properties.TryAdd(KnownProperties.Resource.ExitCode, new ResourcePropertyViewModel(KnownProperties.Resource.ExitCode, Value.ForNumber((double)exitCode), false, null, 0, new BrowserTimeProvider(new NullLoggerFactory())));
        }

        var localizer = new TestStringLocalizer<Columns>();

        // Act
        var tooltip = ResourceStateViewModel.GetResourceStateTooltip(resource, localizer);
        var vm = ResourceStateViewModel.GetStateViewModel(resource, localizer);

        // Assert
        Assert.Equal(expectedTooltip, tooltip);

        Assert.Equal(expectedIconName, vm.Icon.Name);
        Assert.Equal(expectedColor, vm.Color);
        Assert.Equal(expectedText, vm.Text);
    }
}
