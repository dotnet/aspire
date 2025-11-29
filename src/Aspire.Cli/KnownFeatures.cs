// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Cli;

// this is a copy of Shared/KnownResourceNames.cs
internal static class KnownFeatures
{
    public static string FeaturePrefix => "features";
    public static string UpdateNotificationsEnabled => "updateNotificationsEnabled";
    public static string MinimumSdkCheckEnabled => "minimumSdkCheckEnabled";
    public static string ExecCommandEnabled => "execCommandEnabled";
    public static string McpCommandEnabled => "mcpCommandEnabled";
    public static string OrphanDetectionWithTimestampEnabled => "orphanDetectionWithTimestampEnabled";
    public static string ShowDeprecatedPackages => "showDeprecatedPackages";
    public static string PackageSearchDiskCachingEnabled => "packageSearchDiskCachingEnabled";
    public static string StagingChannelEnabled => "stagingChannelEnabled";
    public static string DefaultWatchEnabled => "defaultWatchEnabled";
    public static string ShowAllTemplates => "showAllTemplates";
    public static string DotNetSdkInstallationEnabled => "dotnetSdkInstallationEnabled";
    public static string NonInteractiveSdkInstall => "nonInteractiveSdkInstall";
}
