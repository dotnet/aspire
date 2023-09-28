// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting;

public static class ExecutableComponentBuilderExtensions
{
    public static IDistributedApplicationComponentBuilder<ExecutableComponent> AddExecutable(this IDistributedApplicationBuilder builder, string name, string command, string workingDirectory, params string[]? args)
    {
        var component = new ExecutableComponent(command, workingDirectory, args);
        var componentBuilder = builder.AddComponent(name, component);
        return componentBuilder;
    }
}
