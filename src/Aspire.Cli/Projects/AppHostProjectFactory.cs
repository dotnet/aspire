// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Configuration;
using Microsoft.Extensions.Logging;

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
    private readonly ILogger<AppHostProjectFactory> _logger;

    public AppHostProjectFactory(
        DotNetAppHostProject dotNetProject,
        Func<LanguageInfo, GuestAppHostProject> guestProjectFactory,
        ILanguageDiscovery languageDiscovery,
        IFeatures features,
        ILogger<AppHostProjectFactory> logger)
    {
        _dotNetProject = dotNetProject;
        _guestProjectFactory = guestProjectFactory;
        _languageDiscovery = languageDiscovery;
        _features = features;
        _logger = logger;
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
        _logger.LogDebug("TryGetProject called for file: {AppHostFile}", appHostFile.FullName);
        
        var language = _languageDiscovery.GetLanguageByFile(appHostFile);
        if (language is null)
        {
            _logger.LogDebug("No language found for file: {AppHostFile}", appHostFile.FullName);
            return null;
        }

        _logger.LogDebug("Language detected: {LanguageId} for file: {AppHostFile}", language.LanguageId.Value, appHostFile.FullName);

        // C# is always enabled, guest languages require feature flag
        if (!language.LanguageId.Value.Equals(KnownLanguageId.CSharp, StringComparison.OrdinalIgnoreCase))
        {
            var polyglotEnabled = _features.IsFeatureEnabled(KnownFeatures.PolyglotSupportEnabled, false);
            _logger.LogDebug("Polyglot support enabled: {PolyglotEnabled}", polyglotEnabled);
            
            if (!polyglotEnabled)
            {
                _logger.LogWarning("Skipping {Language} apphost because polyglot support is disabled (features:polyglotSupportEnabled=false): {AppHostFile}", 
                    language.DisplayName, appHostFile.FullName);
                return null;
            }
            
            _logger.LogDebug("Polyglot apphost accepted: {Language} at {AppHostFile}", language.DisplayName, appHostFile.FullName);
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
