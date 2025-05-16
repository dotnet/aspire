// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Cli.Backchannel;

internal class CliRpcTarget
{
#pragma warning disable CA1822 // Mark members as static
    public async Task<string> GetParameterValue(string parameterName, CancellationToken cancellationToken)
#pragma warning restore CA1822 // Mark members as static
    {
        if (GetParameterValueCallback is null)
        {
            throw new InvalidOperationException("GetParameterValueCallback is not set.");
        }

        return await GetParameterValueCallback(parameterName).WaitAsync(cancellationToken).ConfigureAwait(false);
    }

    public Func<string, Task<string>>? GetParameterValueCallback { get; set; } = null!;
}