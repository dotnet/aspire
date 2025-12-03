// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.ExceptionServices;
using Microsoft.Extensions.Logging.Abstractions;

namespace Aspire.Hosting.Tests.Utils;

public static class EnvironmentVariableEvaluator
{
    public static async ValueTask<Dictionary<string, string>> GetEnvironmentVariablesAsync(
        IResource resource,
        DistributedApplicationOperation applicationOperation = DistributedApplicationOperation.Run,
        IServiceProvider? serviceProvider = null)
    {
        var executionContext = new DistributedApplicationExecutionContext(new DistributedApplicationExecutionContextOptions(applicationOperation)
        {
            ServiceProvider = serviceProvider
        });

        (var executionConfiguration, var exception) = await ResourceExecutionConfigurationBuilder.Create(resource, NullLogger.Instance)
            .WithEnvironmentVariables()
            .BuildAsync(executionContext, CancellationToken.None);

        if (exception is not null)
        {
            ExceptionDispatchInfo.Throw(exception);
        }

        return executionConfiguration.EnvironmentVariables.ToDictionary();
    }
}
