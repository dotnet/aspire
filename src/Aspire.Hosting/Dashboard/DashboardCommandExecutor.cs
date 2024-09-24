// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Dashboard;

/// <summary>
/// Used to execute annotations in the dashboard.
/// Although commands are received by the dashboard host, it's important that they're executed
/// in the context of the app host service provider. That allows commands to access user registered services.
/// </summary>
internal sealed class DashboardCommandExecutor
{
    private readonly IServiceProvider _appHostServiceProvider;

    public DashboardCommandExecutor(IServiceProvider appHostServiceProvider)
    {
        _appHostServiceProvider = appHostServiceProvider;
    }

    public async Task<ExecuteCommandResult> ExecuteCommandAsync(string resourceId, ResourceCommandAnnotation annotation, CancellationToken cancellationToken)
    {
        var context = new ExecuteCommandContext
        {
            ResourceName = resourceId,
            ServiceProvider = _appHostServiceProvider,
            CancellationToken = cancellationToken
        };

        return await annotation.ExecuteCommand(context).ConfigureAwait(false);
    }
}
