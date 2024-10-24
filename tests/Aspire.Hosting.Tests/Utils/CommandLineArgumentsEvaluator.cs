// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Tests.Utils;

public static class CommandLineArgumentsEvaluator
{
    public static async ValueTask<IReadOnlyList<string>> GetCommandLineArgumentsAsync(IResource resource,
        DistributedApplicationOperation applicationOperation = DistributedApplicationOperation.Run,
        IServiceProvider? serviceProvider = null, string containerHostName = "host.docker.internal")
    {
        var commandLineArgs = new List<string>();

        if (resource.TryGetAnnotationsOfType<CommandLineArgsCallbackAnnotation>(out var callbacks))
        {
            var config = new List<object>();
            var executionContext = serviceProvider switch
            {
                { } => new DistributedApplicationExecutionContext(new DistributedApplicationExecutionContextOptions(applicationOperation)
                {
                    ServiceProvider = serviceProvider
                }),
                _ => new DistributedApplicationExecutionContext(applicationOperation)
            };

            var context = new CommandLineArgsCallbackContext(config);

            foreach (var callback in callbacks)
            {
                await callback.Callback(context);
            }

            foreach (var arg in config)
            {
                var value = (applicationOperation, arg) switch
                {
                    (_, string s) => s,
                    (DistributedApplicationOperation.Run, IValueProvider provider) => await ExpressionResolver.ResolveAsync(resource.IsContainer(), provider, containerHostName, CancellationToken.None),
                    (DistributedApplicationOperation.Publish, IManifestExpressionProvider provider) => provider.ValueExpression,
                    (_, null) => null,
                    _ => throw new InvalidOperationException($"Unsupported expression type: {arg.GetType()}")
                };

                if (value is not null)
                {
                    commandLineArgs.Add(value);
                }
            }
        }

        return commandLineArgs;
    }
}
