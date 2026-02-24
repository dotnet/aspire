// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Templating;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding templating resources to a distributed application.
/// </summary>
public static class TemplatingBuilderExtensions
{
    /// <summary>
    /// Adds a template prompt resource that collects user input during template application.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the prompt resource.</param>
    /// <param name="promptText">The text displayed to the user.</param>
    /// <param name="inputType">The type of input to collect. Defaults to <see cref="InputType.Text"/>.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/> for chaining.</returns>
    public static IResourceBuilder<TemplatePromptResource> AddTemplatePrompt(
        this IDistributedApplicationBuilder builder,
        string name,
        string promptText,
        InputType inputType = InputType.Text)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(promptText);

        var resource = new TemplatePromptResource(name, promptText, inputType);
        return builder.AddResource(resource);
    }

    /// <summary>
    /// Sets the default value for a template prompt.
    /// </summary>
    /// <param name="builder">The resource builder.</param>
    /// <param name="defaultValue">The default value to display in the prompt.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/> for chaining.</returns>
    public static IResourceBuilder<TemplatePromptResource> WithDefaultValue(
        this IResourceBuilder<TemplatePromptResource> builder,
        string defaultValue)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Resource.DefaultValue = defaultValue;
        return builder;
    }
}
