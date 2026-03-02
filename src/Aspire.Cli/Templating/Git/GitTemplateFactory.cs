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

    public Task<IEnumerable<ITemplate>> GetTemplatesAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(GetTemplatesForScope("new"));
    }

    public Task<IEnumerable<ITemplate>> GetInitTemplatesAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(GetTemplatesForScope("init"));
    }

    private IEnumerable<ITemplate> GetTemplatesForScope(string scope)
    {
        var templates = _indexService.GetTemplatesAsync().GetAwaiter().GetResult();

        foreach (var resolved in templates)
        {
            var entryScope = resolved.Entry.Scope ?? ["new"];
            if (!entryScope.Contains(scope, StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }

            yield return new GitTemplate(
                resolved,
                _engine,
                _interactionService,
                _httpClientFactory,
                _templateLogger);
        }
    }
}
