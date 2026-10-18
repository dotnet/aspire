// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Components.Controls;
using Aspire.Dashboard.Components.Tests.Shared;
using Aspire.Dashboard.Model;
using Aspire.Dashboard.Otlp.Storage;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Aspire.Dashboard.Components.Tests.Controls;

[UseCulture("en-US")]
public sealed class TelemetryEmptyContentTests : DashboardTestContext
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Render_HelpOnlyAvailableForWritableRuns(bool readOnly)
    {
        TelemetryHelpSetupHelpers.SetupDialogs(this);
        await FluentUISetupHelpers.ConfigureTelemetryRepository(this, readOnly, _ => Task.CompletedTask);

        var cut = Render<TelemetryEmptyContent>(parameters => parameters.AddChildContent("<span>No telemetry</span>"));

        Assert.Equal("No telemetry", cut.Find(".telemetry-empty-content > div").TextContent);
        Assert.Equal(readOnly ? 0 : 1, cut.FindAll(".telemetry-help-message").Count);
        Assert.Equal(readOnly ? 0 : 1, cut.FindAll(".telemetry-help-button").Count);
    }

    [Fact]
    public void Render_FirstTimeSetupMessageIncludesDialogLink()
    {
        TelemetryHelpSetupHelpers.SetupDialogs(this);

        var cut = Render<TelemetryEmptyContent>(parameters => parameters.AddChildContent("No telemetry"));

        Assert.Collection(cut.FindAll(".telemetry-help-message > div"),
            line => Assert.Equal("Configure your app to send telemetry to the dashboard.", line.TextContent),
            line => Assert.Equal("For setup instructions, click here.", string.Join(' ', line.TextContent.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))));
        var button = cut.Find(".telemetry-help-button");
        Assert.Equal("here", button.TextContent.Trim());
        Assert.Equal("dialog", button.GetAttribute("aria-haspopup"));
    }

    [Fact]
    public async Task HelpButton_OpensDialog()
    {
        var service = TelemetryHelpSetupHelpers.SetupDialogs(this);
        var cut = Render<TelemetryEmptyContent>(parameters => parameters.AddChildContent("No telemetry"));

        await cut.Find(".telemetry-help-button").ClickAsync(new());

        Assert.Equal(Resources.Dialogs.TelemetryHelpDialogTitle, service.LastInstance!.Options.Header.Title);
        Assert.Equal("700px", service.LastInstance.Options.Width);
        Assert.Equal(Resources.Dialogs.DialogCloseButtonText, service.LastInstance.Options.Footer.PrimaryAction.Label);
        await service.LastInstance.CloseAsync();
    }

    [Fact]
    public async Task Render_ReceiptPersistsAcrossComponentsAndClearing()
    {
        TelemetryHelpSetupHelpers.SetupDialogs(this);
        var repository = Services.GetRequiredService<SqliteTelemetryRepository>();
        await TelemetryHelpSetupHelpers.AddSignalAsync(repository, AspireDataType.StructuredLogs);
        await repository.ClearStructuredLogsAsync();

        var first = Render<TelemetryEmptyContent>(parameters => parameters.AddChildContent("No telemetry"));
        var second = Render<TelemetryEmptyContent>(parameters => parameters.AddChildContent("No telemetry"));

        Assert.Empty(first.FindAll(".telemetry-help-button"));
        Assert.Empty(second.FindAll(".telemetry-help-button"));
        Assert.Empty(first.FindAll(".telemetry-help-message"));
        Assert.Empty(second.FindAll(".telemetry-help-message"));
    }
}
