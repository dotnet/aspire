// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.AWS.Lambda;

/// <summary>
///
/// </summary>
/// <seealso cref="ILambdaFunctionMetadata"/>
internal sealed class LambdaFunctionMetadata : ILambdaFunctionMetadata
{
    /// <summary>
    ///
    /// </summary>
    /// <param name="path"></param>
    /// <param name="handler"></param>
    public LambdaFunctionMetadata(string path, string handler)
    {
        ProjectPath = path;
        Handler = handler;
    }

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
    public string? OutputPath => null;
    /// <summary>
    ///
    /// </summary>
    public string[] Traits => [];
}
