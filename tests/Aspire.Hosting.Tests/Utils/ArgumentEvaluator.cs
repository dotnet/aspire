// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Tests.Utils;

public sealed class ArgumentEvaluator
{
    public static async ValueTask<List<string>> GetArgumentListAsync(IResource resource)
    {
        var finalArgs = new List<string>();

        if (resource.TryGetAnnotationsOfType<CommandLineArgsCallbackAnnotation>(out var exeArgsCallbacks))
        {
            var args = new List<object>();
            var commandLineContext = new CommandLineArgsCallbackContext(args, default);

            foreach (var exeArgsCallback in exeArgsCallbacks)
            {
                await exeArgsCallback.Callback(commandLineContext).ConfigureAwait(false);
            }

            foreach (var arg in args)
            {
                var value = arg switch
                {
                    string s => s,
                    IValueProvider valueProvider => await valueProvider.GetValueAsync().ConfigureAwait(false),
                    null => null,
                    _ => throw new InvalidOperationException($"Unexpected value for {arg}")
                };

                if (value is not null)
                {
                    finalArgs.Add(value);
                }
            }
        }

        return finalArgs;
    }
}
