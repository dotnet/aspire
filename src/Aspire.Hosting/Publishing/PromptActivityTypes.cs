// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Aspire.Hosting.Publishing;

/// <summary>
/// Represents the type of prompt activity.
/// </summary>
[Experimental("ASPIREPUBLISHERS001", UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
public enum PromptActivityType
{
    /// <summary>
    /// Prompt for a string input from the user.
    /// </summary>
    PromptForString,

    /// <summary>
    /// Prompt for a confirmation (yes/no) from the user.
    /// </summary>
    PromptForConfirmation,

    /// <summary>
    /// Prompt for a selection from multiple choices.
    /// </summary>
    PromptForSelection
}

/// <summary>
/// Represents data for a string prompt.
/// </summary>
[Experimental("ASPIREPUBLISHERS001", UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
public sealed record PromptForStringData
{
    /// <summary>
    /// The prompt text to display to the user.
    /// </summary>
    public required string PromptText { get; init; }

    /// <summary>
    /// The default value for the prompt.
    /// </summary>
    public string? DefaultValue { get; init; }
}

/// <summary>
/// Represents data for a confirmation prompt.
/// </summary>
[Experimental("ASPIREPUBLISHERS001", UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
public sealed record PromptForConfirmationData
{
    /// <summary>
    /// The prompt text to display to the user.
    /// </summary>
    public required string PromptText { get; init; }

    /// <summary>
    /// The default value for the confirmation.
    /// </summary>
    public bool DefaultValue { get; init; } = true;
}

/// <summary>
/// Represents data for a selection prompt.
/// </summary>
[Experimental("ASPIREPUBLISHERS001", UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
public sealed record PromptForSelectionData
{
    /// <summary>
    /// The prompt text to display to the user.
    /// </summary>
    public required string PromptText { get; init; }

    /// <summary>
    /// The choices available for selection.
    /// </summary>
    public required string[] Choices { get; init; }

    /// <summary>
    /// Whether multiple selections are allowed.
    /// </summary>
    public bool AllowMultiple { get; init; }
}
