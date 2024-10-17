// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Frozen;
using Aspire.Dashboard.Components.ResourcesGridColumns;
using Aspire.Dashboard.Model;
using Aspire.Dashboard.Resources;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.FluentUI.AspNetCore.Components;
using Xunit;
using Enum = System.Enum;

namespace Aspire.Dashboard.Components.Tests.Controls;

public class StateColumnDisplayTests
{
    private const string ResourceType = "TestResourceType";
    private const string ExitedUnexpectedlyTooltip = "Exited Unexpectedly {0} {1}";
    private const string ExitedTooltip = "Exited {0}";
    private const string RunningAndUnhealthyTooltip = "Running and Unhealthy";
    private const string UnknownStateLabel = "Unknown";

    [Theory]
    // Resource is no longer running
    [InlineData(
        /* state */ KnownResourceState.Exited, null, null,null,
        /* expected output */ "Exited TestResourceType", "Warning", Color.Warning, "Exited")]
    [InlineData(
        /* state */ KnownResourceState.Exited, 3, null, null,
        /* expected output */ "Exited Unexpectedly TestResourceType 3", "ErrorCircle", Color.Error, "Exited")]
    [InlineData(
        /* state */ KnownResourceState.Finished, 0, null, null,
        /* expected output */ "Exited TestResourceType", "RecordStop", Color.Info, "Finished")]
    [InlineData(
        /* state */ KnownResourceState.Unknown, null, null, null,
        /* expected output */ null, "CircleHint", Color.Info, "Unknown")]
    // Health checks
    [InlineData(
        /* state */ KnownResourceState.Running, null, "Healthy", null,
        /* expected output */ null, "CheckmarkCircle", Color.Success, "Running")]
    [InlineData(
        /* state */ KnownResourceState.Running, null, null, null,
        /* expected output */ null, "CheckmarkCircle", Color.Success, "Running")]
    [InlineData(
        /* state */ KnownResourceState.Running, null, "Unhealthy", null,
        /* expected output */ RunningAndUnhealthyTooltip, "CheckmarkCircleWarning", Color.Neutral, "Running (Unhealthy)")]
    [InlineData(
        /* state */ KnownResourceState.Running, null, null, "warning",
        /* expected output */ null, "Warning", Color.Warning, "Running")]
    [InlineData(
        /* state */ KnownResourceState.Running, null, null, "NOT_A_VALID_STATE_STYLE",
        /* expected output */ null, "Circle", Color.Neutral, "Running")]
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
        HealthStatus? healthStatus = healthStatusString is null ? null : Enum.Parse<HealthStatus>(healthStatusString);
        var propertiesDictionary = new Dictionary<string, ResourcePropertyViewModel>();
        if (exitCode is not null)
        {
            propertiesDictionary.TryAdd(KnownProperties.Resource.ExitCode, new ResourcePropertyViewModel(KnownProperties.Resource.ExitCode, Value.ForNumber((double)exitCode), false, null, 0, new BrowserTimeProvider(new NullLoggerFactory())));
        }

        var resource = new ResourceViewModel
        {
            // these are the properties that affect this column
            State = state.ToString(),
            KnownState = state,
            HealthStatus = healthStatus,
            StateStyle = stateStyle,
            Properties = propertiesDictionary.ToFrozenDictionary(),

            // these properties don't matter
            Name = string.Empty,
            ResourceType = ResourceType,
            DisplayName = string.Empty,
            Uid = string.Empty,
            CreationTimeStamp = null,
            StartTimeStamp = null,
            StopTimeStamp = null,
            Environment = default,
            Urls = default,
            Volumes = default,
            Commands = default,
            HealthReports = default
        };

        if (exitCode is not null)
        {
            resource.Properties.TryAdd(KnownProperties.Resource.ExitCode, new ResourcePropertyViewModel(KnownProperties.Resource.ExitCode, Value.ForNumber((double)exitCode), false, null, 0, new BrowserTimeProvider(new NullLoggerFactory())));
        }

        var localizer = new TestColumnLocalizer();

        // Act
        var tooltip = StateColumnDisplay.GetResourceStateTooltip(resource, localizer);
        var vm = StateColumnDisplay.GetStateViewModel(resource, UnknownStateLabel);

        // Assert
        Assert.Equal(expectedTooltip, tooltip);

        Assert.Equal(expectedIconName, vm.Icon.Name);
        Assert.Equal(expectedColor, vm.Color);
        Assert.Equal(expectedText, vm.Text);
    }

    private sealed class TestColumnLocalizer : IStringLocalizer<Columns>
    {
        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
        {
            throw new NotImplementedException();
        }

        public LocalizedString this[string name] => name switch
        {
            nameof(Columns.StateColumnResourceExitedUnexpectedly) => new LocalizedString(nameof(Columns.StateColumnResourceExitedUnexpectedly), ExitedUnexpectedlyTooltip),
            nameof(Columns.StateColumnResourceExited) => new LocalizedString(nameof(Columns.StateColumnResourceExited), ExitedTooltip),
            nameof(Columns.RunningAndUnhealthyResourceStateToolTip) => new LocalizedString(nameof(Columns.RunningAndUnhealthyResourceStateToolTip), RunningAndUnhealthyTooltip),
            _ => throw new ArgumentOutOfRangeException(nameof(name), name, null)
        };

        public LocalizedString this[string name, params object[] arguments] => name switch
        {
            nameof(Columns.StateColumnResourceExitedUnexpectedly) => new LocalizedString(string.Format(nameof(Columns.StateColumnResourceExitedUnexpectedly), arguments), ExitedUnexpectedlyTooltip),
            nameof(Columns.StateColumnResourceExited) => new LocalizedString(string.Format(nameof(Columns.StateColumnResourceExited), arguments), ExitedTooltip),
            nameof(Columns.RunningAndUnhealthyResourceStateToolTip) => new LocalizedString(string.Format(nameof(Columns.RunningAndUnhealthyResourceStateToolTip), arguments), RunningAndUnhealthyTooltip),
            _ => throw new ArgumentOutOfRangeException(nameof(name), name, null)
        };
    }
}
