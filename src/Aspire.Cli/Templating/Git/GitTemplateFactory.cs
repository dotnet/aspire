// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Interaction;
using Microsoft.Extensions.Logging;

namespace Aspire.Cli.Templating.Git;

/// <summary>
/// An <see cref="ITemplateFactory"/> that provides templates from git-hosted indexes.
/// </summary>
internal sealed class GitTemplateFactory : ITemplateFactory
{
    private readonly IGitTemplateIndexService _indexService;
    private readonly IGitTemplateEngine _engine;
    private readonly IInteractionService _interactionService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<GitTemplate> _templateLogger;

    public GitTemplateFactory(
        IGitTemplateIndexService indexService,
        IGitTemplateEngine engine,
        IInteractionService interactionService,
        IHttpClientFactory httpClientFactory,
        ILogger<GitTemplate> templateLogger)
    {
        _indexService = indexService;
        _engine = engine;
        _interactionService = interactionService;
        _httpClientFactory = httpClientFactory;
        _templateLogger = templateLogger;
    }

    public IEnumerable<ITemplate> GetTemplates()
    {
        // Synchronously get cached templates (no network calls on the hot path).
        // Templates are populated after 'aspire template refresh' or on first list/search.
        var templates = _indexService.GetTemplatesAsync().GetAwaiter().GetResult();

        foreach (var resolved in templates)
        {
            yield return new GitTemplate(
                resolved,
                _engine,
                _interactionService,
                _httpClientFactory,
                _templateLogger);
        }
    }

    public IEnumerable<ITemplate> GetInitTemplates()
    {
        // Git templates are not used for 'aspire init' — only for 'aspire new'.
        return [];
    }
}
