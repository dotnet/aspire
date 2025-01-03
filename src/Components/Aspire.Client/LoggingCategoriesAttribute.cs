// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire;

/// <summary>
/// Provides information to describe the logging categories produced by a component.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="LoggingCategoriesAttribute"/> class.
/// </remarks>
/// <param name="categories">The list of log categories produced by the component. These categories will show up under the Logging:LogLevel section in appsettings.json.</param>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
public sealed class LoggingCategoriesAttribute(params string[] categories) : Attribute
{

    /// <summary>
    /// The list of log categories produced by the component. These categories will show up under the Logging:LogLevel section in appsettings.json.
    /// </summary>
    public string[] Categories { get; set; } = categories;
}
