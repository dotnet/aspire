// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.AWS.Lambda;

/// <summary>
///
/// </summary>
/// <seealso cref="IResourceAnnotation"/>
public interface ILambdaFunctionMetadata : IResourceAnnotation
{
    /// <summary>
    ///
    /// </summary>
    public string ProjectPath { get; }

    /// <summary>
    ///
    /// </summary>
    public string Handler { get; }

    /// <summary>
    ///
    /// </summary>
    public string? OutputPath { get; }

    /// <summary>
    ///
    /// </summary>
    public string[] Traits { get; }
}
