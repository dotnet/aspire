// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Tests.Utils;

public sealed class ArgumentEvaluator
{
    public static async ValueTask<List<string>> GetArgumentListAsync(IResource resource)
    {
        if (resource is IResourceWithArgs args)
        {
            return [.. await args.GetArgumentValuesAsync(DistributedApplicationOperation.Run)];
        }
        return [];
    }
}
