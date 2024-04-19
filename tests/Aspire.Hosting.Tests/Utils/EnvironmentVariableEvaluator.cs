// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Tests.Utils;

public static class EnvironmentVariableEvaluator
{
    public static async ValueTask<Dictionary<string, string>> GetEnvironmentVariablesAsync(IResource resource,
        DistributedApplicationOperation applicationOperation = DistributedApplicationOperation.Run)
    {
        var environmentVariables = new Dictionary<string, string>();

        if (resource.TryGetEnvironmentVariables(out var callbacks))
        {
            var config = new EnvironmentVariableDictionary();
            var executionContext = new DistributedApplicationExecutionContext(applicationOperation);
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
                    (DistributedApplicationOperation.Run, IValueProvider provider) => await provider.GetValueAsync().ConfigureAwait(false),
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
