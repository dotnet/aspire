// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.JavaScript;

/// <summary>
/// An annotation that specifies the Vite configuration file path for a Vite JavaScript application resource.
/// </summary>
public sealed class ViteConfigAnnotation : IResourceAnnotation
{
    /// <summary>
    /// The path to the Vite configuration file. Relative to the service root.
    /// </summary>
    public required string ConfigPath { get; init; }
}