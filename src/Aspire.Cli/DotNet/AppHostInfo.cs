// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Cli.DotNet;

/// <summary>
/// Information about an Aspire AppHost project extracted via MSBuild.
/// </summary>
internal sealed record AppHostInfo(
    bool IsAspireHost,
    string? AspireHostingVersion,
    string? DcpCliPath,
    string? DcpExtensionsPath,
    string? DcpBinPath,
    string? DashboardPath,
    string? ContainerRuntime);
