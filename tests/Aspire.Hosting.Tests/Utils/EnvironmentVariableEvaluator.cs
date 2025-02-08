// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.ExceptionServices;
using Microsoft.Extensions.Logging.Abstractions;

namespace Aspire.Hosting.Tests.Utils;

public static class EnvironmentVariableEvaluator
{
    public static async ValueTask<Dictionary<string, string>> GetEnvironmentVariablesAsync(IResource resource,
        DistributedApplicationOperation applicationOperation = DistributedApplicationOperation.Run,
        IServiceProvider? serviceProvider = null, string? containerHostName = null)
    {
        var executionContext = new DistributedApplicationExecutionContext(new DistributedApplicationExecutionContextOptions(applicationOperation)
        {
            ServiceProvider = serviceProvider
        });

        var environmentVariables = new Dictionary<string, string>();
        await resource.ProcessEnvironmentVariableValuesAsync(
            executionContext,
            (key, unprocessed, value, ex) =>
            {
                if (ex is not null)
                {
                    ExceptionDispatchInfo.Throw(ex);
                }

                if (value is string s)
                {
                    environmentVariables[key] = s;
                }
            },
            NullLogger.Instance,
            containerHostName: containerHostName);

        return environmentVariables;
    }
}
