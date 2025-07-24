// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Resources;

namespace Aspire.Cli.Templating;

internal sealed class TemplateProvider : ITemplateProvider
{
    private readonly IEnumerable<ITemplateFactory> _factories;

    public TemplateProvider(IEnumerable<ITemplateFactory> factories)
    {
        if (factories == null || !factories.Any())
        {
            throw new ArgumentException(TemplatingStrings.AtLeastOneTemplateFactoryMustBeProvided, nameof(factories));
        }

        _factories = factories;

    }

    public IEnumerable<ITemplate> GetTemplates()
    {
        var templates = _factories.SelectMany(f => f.GetTemplates());
        return templates;
    }
}
