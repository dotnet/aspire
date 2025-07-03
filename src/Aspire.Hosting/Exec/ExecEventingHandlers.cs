// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.DependencyInjection;

namespace Aspire.Hosting.Exec;

internal static class ExecEventingHandlers
{
    public static Task InitializeExecResources(BeforeStartEvent beforeStartEvent, CancellationToken _)
    {
        var execResourceManager = beforeStartEvent.Services.GetRequiredService<ExecResourceManager>();
        var resource = execResourceManager.ConfigureExecResource();

        if (resource is not null)
        {
            beforeStartEvent.Model.Resources.Add(resource);
        }

        return Task.CompletedTask;
    }
}
