// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

public sealed class ParameterResource(string name, Func<Task<string>> callback) : Resource(name)
{
    public Task<string> GetValueAsync(CancellationToken cancellationToken)
    {
        return callback();
    }
}
