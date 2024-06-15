// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Signifies that a parameter represents a model name, e.g. the name of a resource.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
public class ModelNameAttribute(string target) : Attribute
{
    /// <summary>
    /// The target model kind the name is for, e.g. "Resource".
    /// </summary>
    public string Target { get; } = target;
}
