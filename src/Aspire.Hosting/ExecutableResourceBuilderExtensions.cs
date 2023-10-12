// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting;

public static class ExecutableResourceBuilderExtensions
{
    public static IDistributedApplicationResourceBuilder<ExecutableResource> AddExecutable(this IDistributedApplicationBuilder builder, string name, string command, string workingDirectory, params string[]? args)
    {
        var executable = new ExecutableResource(name, command, workingDirectory, args);
        return builder.AddResource(executable);
    }
}
