// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

public class EnvironmentCallbackAnnotation : IResourceAnnotation
{
    public EnvironmentCallbackAnnotation(string name, Func<string> callback)
    {
        Callback = (c) => c.EnvironmentVariables[name] = callback();
    }

    public EnvironmentCallbackAnnotation(Action<Dictionary<string, string>> callback)
    {
        Callback = (c) => callback(c.EnvironmentVariables);
    }

    public EnvironmentCallbackAnnotation(Action<EnvironmentCallbackContext> callback)
    {
        Callback = callback;
    }

    public Action<EnvironmentCallbackContext> Callback { get; private set; }
}
