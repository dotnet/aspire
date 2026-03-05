// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Cli.Templating;

/// <summary>
/// Provides templates available to CLI commands.
/// </summary>
internal interface ITemplateProvider
{
    /// <summary>
    /// Gets templates available to the <c>aspire new</c> command.
    /// </summary>
    /// <returns>The templates available for project creation.</returns>
    Task<IEnumerable<ITemplate>> GetTemplatesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets templates available to the <c>aspire init</c> command.
    /// </summary>
    /// <returns>The templates available for initializing an existing directory.</returns>
    Task<IEnumerable<ITemplate>> GetInitTemplatesAsync(CancellationToken cancellationToken = default);
}
