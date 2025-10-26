// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Text.RegularExpressions;

namespace Aspire.Hosting.Utils;

/// <summary>
/// Class to parse container references (e.g. "mcr.microsoft.com/dotnet/sdk:8.0")
/// </summary>
public sealed partial class ContainerReferenceParser
{
    /// <summary>
    /// Parses a container reference string into its components.
    /// </summary>
    /// <param name="input">The container reference string to parse (e.g., "mcr.microsoft.com/dotnet/sdk:8.0").</param>
    /// <returns>A <see cref="ContainerReference"/> containing the parsed components.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the input is invalid or cannot be parsed.</exception>
    public static ContainerReference Parse(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            throw new ArgumentOutOfRangeException(nameof(input), "repository name must have at least one component");
        }

        var match = ImageNameRegex().Match(input);

        if (!match.Success)
        {
            throw new ArgumentOutOfRangeException(nameof(input), input, "invalid reference format: could not parse container image name");
        }

        return new(
            GetGroupValueOrDefault(match.Groups["registry"]),
            match.Groups["image"].Value,
            GetGroupValueOrDefault(match.Groups["tag"]),
            GetGroupValueOrDefault(match.Groups["digest"])
            );

        static string? GetGroupValueOrDefault(Group group)
            => group.Success ? group.Value : default;
    }

    // Based on https://github.com/microsoft/vscode-docker-extensibility/blob/46d92f93d620de74b6505bf9fc391af592fee2db/packages/vscode-container-client/src/utils/parseDockerLikeImageName.ts#L22
    // with addition of ipv6 registries
    [GeneratedRegex("^((?<registry>((localhost|\\[(?:[a-fA-F0-9:]+)\\]|([\\w-]+(\\.[\\w-]+)+))(:\\d+)?)|([\\w-]+:\\d+))\\/)?(?<image>[\\w-./<>]+)(:(?<tag>[\\w-.<>]+))?(@(?<digest>.+))?$")]
    private static partial Regex ImageNameRegex();
}

/// <summary>
/// Represents a parsed container reference with its registry, image name, tag, and digest components.
/// </summary>
/// <param name="Registry">The registry hostname (e.g., "mcr.microsoft.com"), or null if not specified.</param>
/// <param name="Image">The image name (e.g., "dotnet/sdk").</param>
/// <param name="Tag">The image tag (e.g., "8.0"), or null if not specified.</param>
/// <param name="Digest">The image digest, or null if not specified.</param>
public record struct ContainerReference(string? Registry, string Image, string? Tag, string? Digest)
{
}
