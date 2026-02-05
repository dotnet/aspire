// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.DotNet.Watch;

internal static class PropertyNames
{
    public const string TargetFramework = nameof(TargetFramework);
    public const string TargetFrameworkIdentifier = nameof(TargetFrameworkIdentifier);
    public const string TargetFrameworkMoniker = nameof(TargetFrameworkMoniker);
    public const string TargetPath = nameof(TargetPath);
    public const string EnableDefaultItems = nameof(EnableDefaultItems);
    public const string TargetFrameworks = nameof(TargetFrameworks);
    public const string WebAssemblyHotReloadCapabilities = nameof(WebAssemblyHotReloadCapabilities);
    public const string TargetFrameworkVersion = nameof(TargetFrameworkVersion);
    public const string TargetName = nameof(TargetName);
    public const string IntermediateOutputPath = nameof(IntermediateOutputPath);
    public const string HotReloadAutoRestart = nameof(HotReloadAutoRestart);
    public const string DefaultItemExcludes = nameof(DefaultItemExcludes);
    public const string CustomCollectWatchItems = nameof(CustomCollectWatchItems);
    public const string DotNetWatchBuild = nameof(DotNetWatchBuild);
    public const string DesignTimeBuild = nameof(DesignTimeBuild);
    public const string SkipCompilerExecution = nameof(SkipCompilerExecution);
    public const string ProvideCommandLineArgs = nameof(ProvideCommandLineArgs);
}

internal static class ItemNames
{
    public const string Watch = nameof(Watch);
    public const string AdditionalFiles = nameof(AdditionalFiles);
    public const string Compile = nameof(Compile);
    public const string Content = nameof(Content);
    public const string ProjectCapability = nameof(ProjectCapability);
    public const string ScopedCssInput = nameof(ScopedCssInput);
    public const string StaticWebAssetEndpoint = nameof(StaticWebAssetEndpoint);
}

internal static class MetadataNames
{
    public const string TargetPath = nameof(TargetPath);
    public const string AssetFile = nameof(AssetFile);
    public const string EndpointProperties = nameof(EndpointProperties);
}

internal static class TargetNames
{
    public const string Compile = nameof(Compile);
    public const string CompileDesignTime = nameof(CompileDesignTime);
    public const string Restore = nameof(Restore);
    public const string ResolveScopedCssInputs = nameof(ResolveScopedCssInputs);
    public const string ResolveReferencedProjectsStaticWebAssets = nameof(ResolveReferencedProjectsStaticWebAssets);
    public const string GenerateComputedBuildStaticWebAssets = nameof(GenerateComputedBuildStaticWebAssets);
    public const string ReferenceCopyLocalPathsOutputGroup = nameof(ReferenceCopyLocalPathsOutputGroup);
}

internal static class ProjectCapability
{
    public const string Aspire = nameof(Aspire);
    public const string AspNetCore = nameof(AspNetCore);
    public const string WebAssembly = nameof(WebAssembly);
}
