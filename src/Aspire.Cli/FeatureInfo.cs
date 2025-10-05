// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Cli;

internal sealed class FeatureInfo
{
    public required string Key { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    
    public static IReadOnlyList<FeatureInfo> KnownFeatureInfos { get; } = new List<FeatureInfo>
    {
        new()
        {
            Key = $"{KnownFeatures.FeaturePrefix}:{KnownFeatures.UpdateNotificationsEnabled}",
            Name = "Update Notifications",
            Description = "Enable notifications when a newer version of the Aspire CLI is available"
        },
        new()
        {
            Key = $"{KnownFeatures.FeaturePrefix}:{KnownFeatures.MinimumSdkCheckEnabled}",
            Name = "Minimum SDK Check",
            Description = "Enable validation that the minimum required .NET SDK version is installed"
        },
        new()
        {
            Key = $"{KnownFeatures.FeaturePrefix}:{KnownFeatures.ExecCommandEnabled}",
            Name = "Exec Command",
            Description = "Enable the experimental 'exec' command for executing commands in running containers"
        },
        new()
        {
            Key = $"{KnownFeatures.FeaturePrefix}:{KnownFeatures.OrphanDetectionWithTimestampEnabled}",
            Name = "Orphan Detection with Timestamp",
            Description = "Enable timestamp-based orphan detection for Docker resources"
        },
        new()
        {
            Key = $"{KnownFeatures.FeaturePrefix}:{KnownFeatures.ShowDeprecatedPackages}",
            Name = "Show Deprecated Packages",
            Description = "Show deprecated packages in search results and recommendations"
        },
        new()
        {
            Key = $"{KnownFeatures.FeaturePrefix}:{KnownFeatures.SingleFileAppHostEnabled}",
            Name = "Single File AppHost",
            Description = "Enable support for single-file AppHost applications"
        },
        new()
        {
            Key = $"{KnownFeatures.FeaturePrefix}:{KnownFeatures.PackageSearchDiskCachingEnabled}",
            Name = "Package Search Disk Caching",
            Description = "Enable disk caching for package search results to improve performance"
        },
        new()
        {
            Key = $"{KnownFeatures.FeaturePrefix}:{KnownFeatures.StagingChannelEnabled}",
            Name = "Staging Channel",
            Description = "Enable access to staging/pre-release packages and templates"
        },
        new()
        {
            Key = $"{KnownFeatures.FeaturePrefix}:{KnownFeatures.DefaultWatchEnabled}",
            Name = "Default Watch Mode",
            Description = "Enable watch mode by default when running projects for automatic rebuilds on file changes"
        }
    };
}
