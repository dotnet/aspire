// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Cli.AppHostRunning;

/// <summary>
/// Factory for creating AppHost runners based on the AppHost file type.
/// </summary>
internal interface IAppHostRunnerFactory
{
    /// <summary>
    /// Creates an appropriate runner for the given AppHost file.
    /// </summary>
    /// <param name="appHostFile">The AppHost file to create a runner for.</param>
    /// <returns>An instance of <see cref="IAppHostRunner"/> capable of running the specified AppHost.</returns>
    IAppHostRunner CreateRunner(FileInfo appHostFile);
}
