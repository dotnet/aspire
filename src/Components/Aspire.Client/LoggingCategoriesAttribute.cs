// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire;

/// <summary>
/// Provides information to describe the logging categories produced by a component.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
public sealed class LoggingCategoriesAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LoggingCategoriesAttribute"/> class.
    /// </summary>
    /// <param name="categories">The list of log categories produced by the component. These categories will show up under the Logging:LogLevel section in appsettings.json.</param>
    public LoggingCategoriesAttribute(params string[] categories)
    {
        Categories = categories;
    }

    /// <summary>
    /// The list of log categories produced by the component. These categories will show up under the Logging:LogLevel section in appsettings.json.
    /// </summary>
    public string[] Categories { get; set; }
}
