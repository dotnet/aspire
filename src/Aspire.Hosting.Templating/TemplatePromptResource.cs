// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Templating;

/// <summary>
/// Represents a template prompt that collects user input during template application.
/// </summary>
/// <param name="name">The name of the prompt resource.</param>
/// <param name="promptText">The text displayed to the user when prompting for input.</param>
/// <param name="inputType">The type of input to collect.</param>
public class TemplatePromptResource(string name, string promptText, InputType inputType) : Resource(name)
{
    /// <summary>
    /// Gets the text displayed to the user when prompting for input.
    /// </summary>
    public string PromptText { get; } = promptText;

    /// <summary>
    /// Gets the type of input to collect from the user.
    /// </summary>
    public InputType InputType { get; } = inputType;

    /// <summary>
    /// Gets or sets the value provided by the user after the prompt is completed.
    /// </summary>
    public string? Value { get; set; }

    /// <summary>
    /// Gets or sets the default value for the prompt.
    /// </summary>
    public string? DefaultValue { get; set; }
}
