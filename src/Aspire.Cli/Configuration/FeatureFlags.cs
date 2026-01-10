// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Cli.Configuration;

/// <summary>
/// Feature flag for enabling update notifications.
/// </summary>
internal sealed class UpdateNotificationsEnabledFeature : IFeatureFlag
{
    public string ConfigurationKey => "updateNotificationsEnabled";
    public bool DefaultValue => true;
}

/// <summary>
/// Feature flag for enabling minimum SDK version checking.
/// </summary>
internal sealed class MinimumSdkCheckEnabledFeature : IFeatureFlag
{
    public string ConfigurationKey => "minimumSdkCheckEnabled";
    public bool DefaultValue => true;
}

/// <summary>
/// Feature flag for enabling the exec command.
/// </summary>
internal sealed class ExecCommandEnabledFeature : IFeatureFlag
{
    public string ConfigurationKey => "execCommandEnabled";
    public bool DefaultValue => false;
}

/// <summary>
/// Feature flag for enabling orphan detection with timestamp.
/// </summary>
internal sealed class OrphanDetectionWithTimestampEnabledFeature : IFeatureFlag
{
    public string ConfigurationKey => "orphanDetectionWithTimestampEnabled";
    public bool DefaultValue => true;
}

/// <summary>
/// Feature flag for showing deprecated packages.
/// </summary>
internal sealed class ShowDeprecatedPackagesFeature : IFeatureFlag
{
    public string ConfigurationKey => "showDeprecatedPackages";
    public bool DefaultValue => false;
}

/// <summary>
/// Feature flag for enabling package search disk caching.
/// </summary>
internal sealed class PackageSearchDiskCachingEnabledFeature : IFeatureFlag
{
    public string ConfigurationKey => "packageSearchDiskCachingEnabled";
    public bool DefaultValue => true;
}

/// <summary>
/// Feature flag for enabling the staging channel.
/// </summary>
internal sealed class StagingChannelEnabledFeature : IFeatureFlag
{
    public string ConfigurationKey => "stagingChannelEnabled";
    public bool DefaultValue => false;
}

/// <summary>
/// Feature flag for enabling watch mode by default.
/// </summary>
internal sealed class DefaultWatchEnabledFeature : IFeatureFlag
{
    public string ConfigurationKey => "defaultWatchEnabled";
    public bool DefaultValue => false;
}

/// <summary>
/// Feature flag for showing all templates.
/// </summary>
internal sealed class ShowAllTemplatesFeature : IFeatureFlag
{
    public string ConfigurationKey => "showAllTemplates";
    public bool DefaultValue => false;
}

/// <summary>
/// Feature flag for enabling .NET SDK installation.
/// </summary>
internal sealed class DotNetSdkInstallationEnabledFeature : IFeatureFlag
{
    public string ConfigurationKey => "dotnetSdkInstallationEnabled";
    public bool DefaultValue => true;
}
