// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Resources;
using Microsoft.Extensions.Localization;

namespace Aspire.Dashboard.Model.Assistant.Markdown;

public class AspireEnrichmentOptions
{
    public required AssistantChatDataContext DataContext { get; init; }
    public required IStringLocalizer<ControlsStrings> Loc { get; init; }
}
