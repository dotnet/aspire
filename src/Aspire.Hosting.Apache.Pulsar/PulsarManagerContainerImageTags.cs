// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Apache.Pulsar;

internal static class PulsarManagerContainerImageTags
{
    public const string Registry = "docker.io";
    public const string Image = "apachepulsar/pulsar-manager";
    public const string Tag = "v0.4.0";

    // TODO: Bump after release of Pulsar Manager
    // Below PRs add value:
    // https://github.com/apache/pulsar-manager/pull/565 - adds super-user via env vars
    // https://github.com/apache/pulsar-manager/pull/564 - fixes duplicate super-user seed

    private static readonly Version s_versionThresholdNotSupportingDefaultSuperUserViaEnvVars = new(0, 4, 0);

    /// <summary>
    /// Calculates if provided image supports seeding default super-user into Pulsar Manager
    /// </summary>
    /// <remarks>
    /// Support for seeding default super-user in Pulsar Manager has been added in > v0.4.0
    /// </remarks>
    /// <param name="annotation">The container image annotation.</param>
    /// <returns>True if supported, false if not</returns>
    internal static bool SupportsDefaultSuperUserEnvVars(ContainerImageAnnotation annotation)
    {
        if (annotation.Image != Image)
        {
            return false;
        }
        if (string.IsNullOrWhiteSpace(annotation.Tag))
        {
            return false;
        }

        var versionParts = annotation.Tag
            .TrimStart('v')
            .ToCharArray()
            .Where(c => c != '.')
            .ToArray();

        Version version = new(versionParts[0], versionParts[1], versionParts[2]);

        return version > s_versionThresholdNotSupportingDefaultSuperUserViaEnvVars;
    }
}
