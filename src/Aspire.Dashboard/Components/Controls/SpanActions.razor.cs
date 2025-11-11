// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Components.CustomIcons;
using Aspire.Dashboard.Model;
using Aspire.Dashboard.Model.Assistant;
using Aspire.Dashboard.Model.Assistant.Prompts;
using Aspire.Dashboard.Model.Otlp;
using Aspire.Dashboard.Otlp.Model;
using Aspire.Dashboard.Resources;
using Aspire.Dashboard.Utils;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using Microsoft.FluentUI.AspNetCore.Components;
using Icons = Microsoft.FluentUI.AspNetCore.Components.Icons;

namespace Aspire.Dashboard.Components;

public partial class SpanActions : ComponentBase
{
    private static readonly Icon s_viewDetailsIcon = new Icons.Regular.Size16.Info();
    private static readonly Icon s_structuredLogsIcon = new Icons.Regular.Size16.SlideTextSparkle();
    private static readonly Icon s_gitHubCopilotIcon = new AspireIcons.Size16.GitHubCopilot();

    private AspireMenuButton? _menuButton;

    [Inject]
    public required IStringLocalizer<Resources.ControlsStrings> ControlsLoc { get; init; }

    [Inject]
    public required IStringLocalizer<Resources.AIAssistant> AIAssistantLoc { get; init; }

    [Inject]
    public required IStringLocalizer<Resources.AIPrompts> AIPromptsLoc { get; init; }

    [Inject]
    public required NavigationManager NavigationManager { get; init; }

    [Inject]
    public required IAIContextProvider AIContextProvider { get; init; }

    [Parameter]
    public required EventCallback<string> OnViewDetails { get; set; }

    [Parameter]
    public required SpanWaterfallViewModel SpanViewModel { get; set; }

    private readonly List<MenuButtonItem> _menuItems = new();

    protected override void OnParametersSet()
    {
        _menuItems.Clear();

        _menuItems.Add(new MenuButtonItem
        {
            Text = ControlsLoc[nameof(Resources.ControlsStrings.ActionViewDetailsText)],
            Icon = s_viewDetailsIcon,
            OnClick = () => OnViewDetails.InvokeAsync(_menuButton?.MenuButtonId)
        });
        _menuItems.Add(new MenuButtonItem
        {
            Text = ControlsLoc[nameof(Resources.ControlsStrings.ActionStructuredLogsText)],
            Icon = s_structuredLogsIcon,
            OnClick = () =>
            {
                NavigationManager.NavigateTo(DashboardUrls.StructuredLogsUrl(spanId: SpanViewModel.Span.SpanId));
                return Task.CompletedTask;
            }
        });

        if (AIContextProvider.Enabled)
        {
            _menuItems.Add(new MenuButtonItem
            {
                Text = AIAssistantLoc[nameof(AIAssistant.MenuTextAskGitHubCopilot)],
                Icon = s_gitHubCopilotIcon,
                OnClick = async () =>
                {
                    await AIContextProvider.LaunchAssistantSidebarAsync(
                        promptContext => PromptContextsBuilder.AnalyzeSpan(
                            promptContext,
                            AIPromptsLoc.GetString(nameof(AIPrompts.PromptAnalyzeSpan), OtlpHelpers.ToShortenedId(SpanViewModel.Span.SpanId)),
                            SpanViewModel.Span));
                }
            });
        }
    }
}
