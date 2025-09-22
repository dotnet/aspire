// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Configuration;

namespace Aspire.Cli.Templating;

/// <summary>
/// Template factory for Git repository-based templates.
/// This factory provides templates sourced from Git repositories, conditionally
/// enabled by the GitRepositoryTemplates feature flag.
/// </summary>
/// <param name="features">Feature configuration service to check if Git repository templates are enabled.</param>
internal class GitRepositoryTemplateFactory(IFeatures features) : ITemplateFactory
{
    /// <summary>
    /// Gets the templates available from Git repositories.
    /// Returns an empty collection when the GitRepositoryTemplates feature flag is disabled.
    /// </summary>
    /// <returns>An enumerable of templates from Git repositories, or empty if feature is disabled.</returns>
    public IEnumerable<ITemplate> GetTemplates()
    {
        // Only yield templates if the Git repository templates feature is enabled
        if (!features.IsFeatureEnabled(KnownFeatures.GitRepositoryTemplates, false))
        {
            yield break;
        }

        // TODO: Future implementation will add Git repository template discovery and resolution logic here
        // This is the foundation for Git-backed template functionality

        // For now, return no templates as this is infrastructure setup only
        yield break;
    }
}