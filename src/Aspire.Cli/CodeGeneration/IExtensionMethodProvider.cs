// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.CodeGeneration.Models;

namespace Aspire.Cli.CodeGeneration;

/// <summary>
/// Provides extension method information for code generation.
/// </summary>
internal interface IExtensionMethodProvider
{
    /// <summary>
    /// Gets extension methods for a given package.
    /// </summary>
    List<ExtensionMethodModel> GetExtensionMethods(string packageId);
}
