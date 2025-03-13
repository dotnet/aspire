// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Cli;

internal sealed class AppHostRunner(DotNetCliRunner cli)
{
    public async Task<int> RunAppHostAsync(FileInfo appHostProjectFile, string[] args, CancellationToken cancellationToken)
    {
        return await cli.RunAsync(appHostProjectFile, args, cancellationToken).ConfigureAwait(false);
    }
}