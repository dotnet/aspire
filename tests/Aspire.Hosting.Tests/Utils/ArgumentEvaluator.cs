// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.ExceptionServices;

namespace Aspire.Hosting.Tests.Utils;

public sealed class ArgumentEvaluator
{
    public static async ValueTask<List<string>> GetArgumentListAsync(IResource resource)
    {
        var args = new List<string>();

        await resource.ProcessArgumentValuesAsync(new(DistributedApplicationOperation.Run), (unprocessed, processed, ex) =>
        {
            if (ex is not null)
            {
                ExceptionDispatchInfo.Throw(ex);
            }

            if (processed is string s)
            {
                args.Add(s);
            }
        });

        return args;
    }
}
