// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using Aspire.Dashboard.Components.Controls;
using Aspire.Dashboard.Components.Dialogs;
using Aspire.Dashboard.Components.Tests.Shared;
using Aspire.Dashboard.Configuration;
using Aspire.Dashboard.Model;
using Aspire.Hosting;
using Bunit;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FluentUI.AspNetCore.Components;
using Xunit;
using Icons = Microsoft.FluentUI.AspNetCore.Components.Icons;

namespace Aspire.Dashboard.Components.Tests.Dialogs;

[UseCulture("en-US")]
public sealed class TelemetryHelpDialogTests : DashboardTestContext
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void Render_OnlyEnabledEndpointsAreInMarkdownTable(bool grpc, bool http)
    {
        TelemetryHelpSetupHelpers.SetupDialogs(this, new OtlpEndpointInfo(
            () => grpc ? new ResolvedEndpointInfo(BindingAddress.Parse("https://0.0.0.0:0"), new IPEndPoint(IPAddress.Any, 4317), IsHttps: true) : null,
            () => http ? new ResolvedEndpointInfo(BindingAddress.Parse("http://[::]:0"), new IPEndPoint(IPAddress.IPv6Any, 4318), IsHttps: false) : null));

        var cut = Render<TelemetryHelpDialog>();
        var expected = new List<(string Protocol, string Endpoint)>();
        if (grpc)
        {
            expected.Add(("OTLP/gRPC", "https://localhost:4317"));
        }
        if (http)
        {
            expected.Add(("OTLP/HTTP", "http://localhost:4318"));
        }
        Assert.Equal(expected, cut.FindAll(".markdown-container tbody tr").Select(row =>
            (row.Children[0].TextContent, row.Children[1].TextContent)));
        Assert.Equal(expected.Select(endpoint => endpoint.Endpoint), cut.FindAll("td code").Select(code => code.TextContent));
        Assert.Equal(grpc || http ? ["Protocol", "Dashboard endpoint"] : [], cut.FindAll("th").Select(header => header.TextContent));
        Assert.Equal(grpc || http ? 1 : 0, cut.FindAll("table").Count);
        Assert.Equal(!(grpc || http), cut.Markup.Contains(Resources.Dialogs.TelemetryHelpNoEndpointsDescription, StringComparison.Ordinal));
        Assert.Equal(
            [
                "https://opentelemetry.io/docs/specs/otlp/",
                "https://opentelemetry.io/docs/languages/sdk-configuration/otlp-exporter/",
                "https://aka.ms/aspire/dashboard-manage-data"
            ],
            cut.FindAll("a").Select(link => link.GetAttribute("href")));
        Assert.All(cut.FindAll("a"), link =>
        {
            Assert.Equal("noopener noreferrer nofollow", link.GetAttribute("rel"));
            Assert.Equal("_blank", link.GetAttribute("target"));
        });
    }

    [Fact]
    public void Render_HelpUsesMarkdownAndManageDataButtonIsLast()
    {
        TelemetryHelpSetupHelpers.SetupDialogs(this);

        var cut = Render<TelemetryHelpDialog>();

        Assert.Single(cut.FindComponents<MarkdownRenderer>());
        Assert.Equal(
            ["📡 Send telemetry from your app", "📥 Import telemetry from a file"],
            cut.FindAll(".markdown-container h4").Select(heading => heading.TextContent));
        Assert.Equal(
            [
                "Visualize logs, traces, and metrics in the dashboard. To see telemetry, first send it to the dashboard using OTLP or manually import telemetry JSON files.",
                "Configure your app to export logs, traces, and metrics using OTLP.",
                Resources.Dialogs.TelemetryHelpNoEndpointsDescription,
                "Configure an OpenTelemetry SDK with OTLP",
                "Import previously exported telemetry or OTLP JSON files by selecting Import logs and telemetry in the Manage logs and telemetry dialog.",
                "Learn about managing dashboard data"
            ],
            cut.FindAll(".markdown-container p").Select(paragraph => paragraph.TextContent));
        var manageDataButton = cut.Find(".telemetry-help-content > .markdown-container + .telemetry-import-button:last-child");
        Assert.Equal(Resources.Dialogs.ManageDataDialogTitle, manageDataButton.TextContent.Trim());
        Assert.IsType<Icons.Regular.Size16.Settings>(cut.FindComponent<FluentButton>().Instance.IconStart);
    }

    [Fact]
    public void Render_HeaderHasMetricsIcon()
    {
        TelemetryHelpSetupHelpers.SetupDialogs(this);

        var cut = Render<TelemetryHelpDialog>();
        var titleTemplate = cut.FindComponent<FluentDialogBody>().Instance.TitleTemplate;
        Assert.NotNull(titleTemplate);

        var title = Render(titleTemplate);

        Assert.Equal(Resources.Dialogs.TelemetryHelpDialogTitle, title.Find(".telemetry-help-dialog-title").TextContent.Trim());
        var icon = title.FindComponent<FluentIcon<Icons.Regular.Size24.ChartMultiple>>().Instance;
        Assert.Equal(Color.Primary, icon.Color);
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("false", false)]
    [InlineData("true", true)]
    public void Render_ContainerGuidanceRequiresContainerConfiguration(string? value, bool expectedGuidance)
    {
        TelemetryHelpSetupHelpers.SetupDialogs(this);
        Services.GetRequiredService<IConfiguration>()[KnownConfigNames.DotnetRunningInContainer] = value;

        var cut = Render<TelemetryHelpDialog>();

        Assert.Equal(expectedGuidance ? 1 : 0, cut.FindAll(".markdown-container p")
            .Count(paragraph => paragraph.TextContent == "The dashboard is running in a container. Access it from the host using port forwarding."));
    }

    [Theory]
    [InlineData(OtlpAuthMode.Unsecured, true, null)]
    [InlineData(OtlpAuthMode.ApiKey, true, nameof(Resources.Dialogs.TelemetryHelpApiKeyDescription))]
    [InlineData(OtlpAuthMode.ClientCertificate, true, nameof(Resources.Dialogs.TelemetryHelpCertificateDescription))]
    [InlineData(OtlpAuthMode.ApiKey, false, null)]
    public void Render_AuthenticationGuidanceNeverIncludesSecrets(OtlpAuthMode mode, bool hasEndpoint, string? resourceName)
    {
        const string primarySecret = "primary-secret-never-render";
        const string secondarySecret = "secondary-secret-never-render";
        TelemetryHelpSetupHelpers.SetupDialogs(this, new OtlpEndpointInfo(
            () => hasEndpoint ? new ResolvedEndpointInfo(BindingAddress.Parse("https://localhost:4317"), new IPEndPoint(IPAddress.Loopback, 4317), IsHttps: true) : null,
            () => null),
            options =>
            {
                options.Otlp.AuthMode = mode;
                options.Otlp.PrimaryApiKey = primarySecret;
                options.Otlp.SecondaryApiKey = secondarySecret;
            });

        var cut = Render<TelemetryHelpDialog>();

        Assert.Equal(
            resourceName is null ? [] : new[] { Resources.Dialogs.ResourceManager.GetString(resourceName) },
            cut.FindAll(".markdown-container p").Select(paragraph => paragraph.TextContent)
                .Where(text => text == Resources.Dialogs.TelemetryHelpApiKeyDescription || text == Resources.Dialogs.TelemetryHelpCertificateDescription));
        Assert.False(cut.Markup.Contains(primarySecret, StringComparison.Ordinal));
        Assert.False(cut.Markup.Contains(secondarySecret, StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(false, false, true)]
    [InlineData(true, false, false)]
    [InlineData(false, true, false)]
    public async Task Render_ImportRespectsConfigurationAndReadOnlyState(bool disabled, bool readOnly, bool expectedButton)
    {
        TelemetryHelpSetupHelpers.SetupDialogs(this, configure: options => options.UI.DisableImport = disabled);
        await FluentUISetupHelpers.ConfigureTelemetryRepository(this, readOnly, _ => Task.CompletedTask);

        var cut = Render<TelemetryHelpDialog>();

        Assert.Equal(expectedButton ? 1 : 0, cut.FindAll(".telemetry-import-button").Count);
        Assert.Equal(
            expectedButton ? ["📡 Send telemetry from your app", "📥 Import telemetry from a file"] : ["📡 Send telemetry from your app"],
            cut.FindAll(".markdown-container h4").Select(heading => heading.TextContent));
    }

    [Fact]
    public async Task Import_ClosesHelpBeforeOpeningManageData()
    {
        var service = TelemetryHelpSetupHelpers.SetupDialogs(this);
        var dashboardDialogs = Services.GetRequiredService<DashboardDialogService>();
        var helpReference = await dashboardDialogs.ShowDialogAsync<TelemetryHelpDialog>(new DialogParameters
        {
            Title = Resources.Dialogs.TelemetryHelpDialogTitle
        });
        var helpInstance = service.LastInstance!;
        var cut = Render<TelemetryHelpDialog>(parameters => parameters.AddCascadingValue(helpInstance));

        await cut.Find(".telemetry-import-button").ClickAsync(new());

        Assert.True(helpReference.Result.IsCompletedSuccessfully);
        Assert.NotSame(helpInstance, service.LastInstance);
        Assert.Equal(Resources.Dialogs.ManageDataDialogTitle, service.LastInstance!.Options.Header.Title);
        await service.LastInstance.CloseAsync();
    }
}
