// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Specifies the icon to use when displaying a resource in the dashboard.
/// </summary>
[DebuggerDisplay("Type = {GetType().Name,nq}, IconName = {IconName}, IconVariant = {IconVariant}, HasCustomIconData = {CustomIconData != null}")]
public sealed class ResourceIconAnnotation : IResourceAnnotation
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceIconAnnotation"/> class with a FluentUI icon name.
    /// </summary>
    /// <param name="iconName">The name of the FluentUI icon to use.</param>
    /// <param name="iconVariant">The variant of the icon (Regular or Filled).</param>
    public ResourceIconAnnotation(string iconName, IconVariant iconVariant = IconVariant.Filled)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(iconName);
        IconName = iconName;
        IconVariant = iconVariant;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceIconAnnotation"/> class with custom icon data.
    /// </summary>
    /// <param name="customIconData">The custom icon data (SVG content or data URI).</param>
    /// <param name="iconName">Optional icon name for reference.</param>
    public ResourceIconAnnotation(string customIconData, string? iconName = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(customIconData);
        CustomIconData = customIconData;
        IconName = iconName ?? string.Empty;
        IconVariant = IconVariant.Regular;
    }

    /// <summary>
    /// Gets the name of the FluentUI icon to use for the resource.
    /// </summary>
    /// <remarks>
    /// The icon name should be a valid FluentUI icon name when <see cref="CustomIconData"/> is null.
    /// See https://aka.ms/fluentui-system-icons for available icons.
    /// When <see cref="CustomIconData"/> is specified, this serves as a reference name.
    /// </remarks>
    public string IconName { get; }

    /// <summary>
    /// Gets the variant of the icon (Regular or Filled).
    /// </summary>
    /// <remarks>
    /// This property is only used when <see cref="CustomIconData"/> is null and a FluentUI icon name is specified.
    /// </remarks>
    public IconVariant IconVariant { get; }

    /// <summary>
    /// Gets the custom icon data, which can be SVG content or a data URI (e.g., data:image/png;base64,...).
    /// </summary>
    /// <remarks>
    /// When this property is set, it takes precedence over <see cref="IconName"/> for icon display.
    /// The data should be either:
    /// - Raw SVG content (e.g., "&lt;svg width='24' height='24'&gt;&lt;circle cx='12' cy='12' r='10'/&gt;&lt;/svg&gt;")
    /// - A data URI (e.g., "data:image/png;base64,iVBORw0KGgo...")
    /// </remarks>
    public string? CustomIconData { get; }
}