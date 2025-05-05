// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Model;
using Aspire.Dashboard.Otlp.Model;
using Aspire.Dashboard.Resources;
using Aspire.Dashboard.Tests.TelemetryRepositoryTests;
using Aspire.Tests.Shared.DashboardModel;
using Aspire.Tests.Shared.Telemetry;
using Google.Protobuf.Collections;
using Microsoft.AspNetCore.Components;
using OpenTelemetry.Proto.Trace.V1;
using Xunit;

namespace Aspire.Dashboard.Tests.Model;

public sealed class ResourceMenuItemsTests
{
    private static readonly DateTime s_testTime = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void AddMenuItems_NoTelemetry_NoTelemetryItems()
    {
        // Arrange
        var resource = ModelTestHelpers.CreateResource();
        var telemetryRespository = TelemetryTestHelpers.CreateRepository();

        // Act
        var menuItems = new List<MenuButtonItem>();
        ResourceMenuItems.AddMenuItems(
            menuItems,
            openingMenuButtonId: null,
            resource,
            new TestNavigationManager(),
            telemetryRespository,
            r => r.Name,
            new TestStringLocalizer<Resources.ControlsStrings>(),
            new TestStringLocalizer<Resources.Resources>(),
            new TestStringLocalizer<Commands>(),
            _ => Task.CompletedTask,
            _ => Task.CompletedTask,
            (_, _) => false,
            true,
            true);

        // Assert
        Assert.Collection(menuItems,
            e => Assert.Equal("Localized:ActionViewDetailsText", e.Text),
            e => Assert.Equal("Localized:ResourceActionConsoleLogsText", e.Text));
    }

    [Fact]
    public void AddMenuItems_UninstrumentedPeer_TraceItem()
    {
        // Arrange
        var resource = ModelTestHelpers.CreateResource(appName: "test-abc");
        var outgoingPeerResolver = new TestOutgoingPeerResolver(onResolve: attributes => (resource.Name, resource));
        var repository = TelemetryTestHelpers.CreateRepository(outgoingPeerResolvers: [outgoingPeerResolver]);
        var addContext = new AddContext();
        repository.AddTraces(addContext, new RepeatedField<ResourceSpans>()
        {
            new ResourceSpans
            {
                Resource = TelemetryTestHelpers.CreateResource(name: "source", instanceId: "abc"),
                ScopeSpans =
                {
                    new ScopeSpans
                    {
                        Scope = TelemetryTestHelpers.CreateScope(),
                        Spans =
                        {
                            TelemetryTestHelpers.CreateSpan(traceId: "1", spanId: "1-1", startTime: s_testTime.AddMinutes(1), endTime: s_testTime.AddMinutes(10), attributes: [KeyValuePair.Create(OtlpSpan.PeerServiceAttributeKey, "value-1")], kind: Span.Types.SpanKind.Client),
                            TelemetryTestHelpers.CreateSpan(traceId: "1", spanId: "1-2", startTime: s_testTime.AddMinutes(5), endTime: s_testTime.AddMinutes(10), parentSpanId: "1-1", attributes: [KeyValuePair.Create(OtlpSpan.PeerServiceAttributeKey, "value-2")], kind: Span.Types.SpanKind.Client)
                        }
                    }
                }
            }
        });

        // Act
        var menuItems = new List<MenuButtonItem>();
        ResourceMenuItems.AddMenuItems(
            menuItems,
            openingMenuButtonId: null,
            resource,
            new TestNavigationManager(),
            repository,
            r => r.Name,
            new TestStringLocalizer<Resources.ControlsStrings>(),
            new TestStringLocalizer<Resources.Resources>(),
            new TestStringLocalizer<Commands>(),
            _ => Task.CompletedTask,
            _ => Task.CompletedTask,
            (_, _) => false,
            true,
            true);

        // Assert
        Assert.Collection(menuItems,
            e => Assert.Equal("Localized:ActionViewDetailsText", e.Text),
            e => Assert.Equal("Localized:ResourceActionConsoleLogsText", e.Text),
            e => Assert.True(e.IsDivider),
            e => Assert.Equal("Localized:ResourceActionTracesText", e.Text));
    }

    [Fact]
    public void AddMenuItems_HasTelemetry_TelemetryItems()
    {
        // Arrange
        var resource = ModelTestHelpers.CreateResource(appName: "test-abc");
        var repository = TelemetryTestHelpers.CreateRepository();
        var addContext = new AddContext();
        repository.AddTraces(addContext, new RepeatedField<ResourceSpans>()
        {
            new ResourceSpans
            {
                Resource = TelemetryTestHelpers.CreateResource(name: "test", instanceId: "abc"),
                ScopeSpans =
                {
                    new ScopeSpans
                    {
                        Scope = TelemetryTestHelpers.CreateScope(),
                        Spans =
                        {
                            TelemetryTestHelpers.CreateSpan(traceId: "1", spanId: "1-1", startTime: s_testTime.AddMinutes(1), endTime: s_testTime.AddMinutes(10))
                        }
                    }
                }
            }
        });

        // Act
        var menuItems = new List<MenuButtonItem>();
        ResourceMenuItems.AddMenuItems(
            menuItems,
            openingMenuButtonId: null,
            resource,
            new TestNavigationManager(),
            repository,
            r => r.Name,
            new TestStringLocalizer<Resources.ControlsStrings>(),
            new TestStringLocalizer<Resources.Resources>(),
            new TestStringLocalizer<Commands>(),
            _ => Task.CompletedTask,
            _ => Task.CompletedTask,
            (_, _) => false,
            true,
            true);

        // Assert
        Assert.Collection(menuItems,
            e => Assert.Equal("Localized:ActionViewDetailsText", e.Text),
            e => Assert.Equal("Localized:ResourceActionConsoleLogsText", e.Text),
            e => Assert.True(e.IsDivider),
            e => Assert.Equal("Localized:ResourceActionStructuredLogsText", e.Text),
            e => Assert.Equal("Localized:ResourceActionTracesText", e.Text),
            e => Assert.Equal("Localized:ResourceActionMetricsText", e.Text));
    }

    private sealed class TestNavigationManager : NavigationManager
    {
    }
}
