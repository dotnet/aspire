// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Tests.Utils;

public static class EnvironmentVariableEvaluator
{
    public static async ValueTask<Dictionary<string, string>> GetEnvironmentVariablesAsync(IResource resource,
        DistributedApplicationOperation applicationOperation = DistributedApplicationOperation.Run,
        IServiceProvider? serviceProvider = null, string containerHostName = "host.docker.internal")
    {
        var environmentVariables = new Dictionary<string, string>();

        if (resource.TryGetEnvironmentVariables(out var callbacks))
        {
            var config = new Dictionary<string, object>();
            var executionContext = serviceProvider switch
            {
                { } => new DistributedApplicationExecutionContext(new DistributedApplicationExecutionContextOptions(applicationOperation)
                {
                    ServiceProvider = serviceProvider
                }),
                _ => new DistributedApplicationExecutionContext(applicationOperation)
            };

            var context = new EnvironmentCallbackContext(executionContext, config);

            foreach (var callback in callbacks)
            {
                await callback.Callback(context);
            }

            foreach (var (key, expr) in config)
            {
                var value = (applicationOperation, expr) switch
                {
                    (_, string s) => s,
                    (DistributedApplicationOperation.Run, IValueProvider provider) => await ExpressionResolver.ResolveAsync(resource.IsContainer(), provider, containerHostName, CancellationToken.None),
                    (DistributedApplicationOperation.Publish, IManifestExpressionProvider provider) => provider.ValueExpression,
                    (_, null) => null,
                    _ => throw new InvalidOperationException($"Unsupported expression type: {expr.GetType()}")
                };

                if (value is not null)
                {
                    environmentVariables[key] = value;
                }
            }
        }

        return environmentVariables;
    }
}
