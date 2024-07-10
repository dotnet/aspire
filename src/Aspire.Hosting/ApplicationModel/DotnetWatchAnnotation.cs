// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents an annotation for the dotnet watch command.
/// </summary>
/// <param name="enableHotReload"></param>
public sealed class DotnetWatchAnnotation(bool enableHotReload) : IResourceAnnotation
{
    /// <summary>
    /// If set, the dotnet watch command will be called without the --no-hot-reload argument
    /// </summary>
    /// <remarks>In some cases, hot reloads might not be sufficient</remarks>
    public bool EnableHotReload { get; } = enableHotReload;
}
