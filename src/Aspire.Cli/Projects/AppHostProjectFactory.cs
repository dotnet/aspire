// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Configuration;

namespace Aspire.Cli.Projects;

/// <summary>
/// Factory for creating AppHost projects from resolved language information.
/// </summary>
internal sealed class AppHostProjectFactory : IAppHostProjectFactory
{
    private readonly DotNetAppHostProject _dotNetProject;
    private readonly Func<LanguageInfo, GuestAppHostProject> _guestProjectFactory;
    private readonly ILanguageDiscovery _languageDiscovery;
    private readonly IFeatures _features;

    public AppHostProjectFactory(
        DotNetAppHostProject dotNetProject,
        Func<LanguageInfo, GuestAppHostProject> guestProjectFactory,
        ILanguageDiscovery languageDiscovery,
        IFeatures features)
    {
        _dotNetProject = dotNetProject;
        _guestProjectFactory = guestProjectFactory;
        _languageDiscovery = languageDiscovery;
        _features = features;
    }

    /// <inheritdoc />
    public IAppHostProject GetProject(LanguageInfo language)
    {
        if (language.LanguageId.Value.Equals(KnownLanguageId.CSharp, StringComparison.OrdinalIgnoreCase))
        {
            return _dotNetProject;
        }

        return _guestProjectFactory(language);
    }

    /// <inheritdoc />
    public IAppHostProject? TryGetProject(FileInfo appHostFile)
    {
        var language = _languageDiscovery.GetLanguageByFile(appHostFile);
        if (language is null)
        {
            return null;
        }

        // C# is always enabled, guest languages require feature flag
        if (!language.LanguageId.Value.Equals(KnownLanguageId.CSharp, StringComparison.OrdinalIgnoreCase) &&
            !_features.IsFeatureEnabled(KnownFeatures.PolyglotSupportEnabled, false))
        {
            return null;
        }

        return GetProject(language);
    }

    /// <inheritdoc />
    public IAppHostProject GetProject(FileInfo appHostFile)
    {
        var project = TryGetProject(appHostFile);
        if (project is null)
        {
            throw new NotSupportedException($"No handler available for AppHost file '{appHostFile.Name}'.");
        }
        return project;
    }
}
