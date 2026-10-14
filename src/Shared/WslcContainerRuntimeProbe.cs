// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace Aspire.Shared;

/// <summary>
/// Handles Windows-only WSLc support and its session-manager metadata protocol.
/// </summary>
internal sealed class WslcContainerRuntimeProbe() : ContainerRuntimeProbe(KnownContainerRuntimes.Wslc, "wslc", "WSLc", 2)
{
    public override string? GetUnsupportedHostError(bool isWindows)
    {
        return isWindows ? null : "WSLc is only supported on Windows.";
    }

    protected override async Task<RuntimeVersionInfo> ReadVersionInfoAsync(bool isRunning, ILogger? logger, CancellationToken cancellationToken)
    {
        if (!isRunning)
        {
            // Doctor does not consume version metadata for a stopped WSLc session.
            return default;
        }

        return await ReadVersionOutputAsync("info --format json", async (stream, token) =>
        {
            var info = await JsonSerializer.DeserializeAsync(stream, WslcRuntimeJsonContext.Default.WslcRuntimeInfoJson, token).ConfigureAwait(false);
            return NormalizeInfo(info);
        }, logger, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Normalizes WSLc info metadata into the common client/server version result.
    /// </summary>
    internal static RuntimeVersionInfo ParseInfoOutput(string? output)
    {
        if (string.IsNullOrWhiteSpace(output))
        {
            return default;
        }

        try
        {
            // `wslc info --format json` reports:
            // {"Client":{"Version":"3.0.3.0"},"Server":{"SessionManagerVersion":"3.0.3"}}
            var info = JsonSerializer.Deserialize(output, WslcRuntimeJsonContext.Default.WslcRuntimeInfoJson);
            return NormalizeInfo(info);
        }
        catch (JsonException)
        {
            return default;
        }

    }

    private static RuntimeVersionInfo NormalizeInfo(WslcRuntimeInfoJson? info)
    {
        Version.TryParse(info?.Client?.Version, out var clientVersion);
        Version.TryParse(info?.Server?.SessionManagerVersion, out var serverVersion);

        return new RuntimeVersionInfo(clientVersion, serverVersion, false, null);
    }
}

internal sealed class WslcRuntimeInfoJson
{
    public WslcClientVersionJson? Client { get; set; }
    public WslcServerVersionJson? Server { get; set; }
}

internal sealed class WslcClientVersionJson
{
    public string? Version { get; set; }
}

internal sealed class WslcServerVersionJson
{
    public string? SessionManagerVersion { get; set; }
}

[JsonSerializable(typeof(WslcRuntimeInfoJson))]
internal sealed partial class WslcRuntimeJsonContext : JsonSerializerContext
{
}
