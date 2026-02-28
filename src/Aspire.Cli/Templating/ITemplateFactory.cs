// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Cli.Templating;

internal interface ITemplateFactory
{
    Task<IEnumerable<ITemplate>> GetTemplatesAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<ITemplate>> GetInitTemplatesAsync(CancellationToken cancellationToken = default);
}
