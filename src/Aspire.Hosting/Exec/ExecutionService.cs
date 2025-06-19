// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Channels;
using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.Options;

namespace Aspire.Hosting.Exec;

internal class ExecutionService
{
    private readonly ResourceLoggerService _loggerService;
    private readonly ResourceNotificationService _noticationService;

    private readonly ExecOptions _execOptions;
    private readonly DistributedApplicationModel _model;

    private readonly Channel<string> _logChannel = Channel.CreateUnbounded<string>();

    public ExecutionService(
        IOptions<ExecOptions> execOptions,
        DistributedApplicationModel model,
        ResourceLoggerService loggerService,
        ResourceNotificationService notificationService)
    {
        _execOptions = execOptions.Value;
        _model = model;

        _loggerService = loggerService;
        _noticationService = notificationService;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var targetResource = _model.Resources.FirstOrDefault(x => x.Name == _execOptions.ResourceName);
        if (targetResource is null)
        {
            throw new ArgumentException($"Can't find resource with {_execOptions.ResourceName} name.");
        }
        if (targetResource is not IResourceSupportsExec targetExecResource)
        {
            throw new ArgumentException($"Resource {_execOptions.ResourceName} does not support exec.");
        }

        // notification service can be used only from DCP because we need resourceId, not resourceName
        //if (!_noticationService.TryGetCurrentState(_execOptions.ResourceName, out var resourceState))
        //{ 
        //    throw new ArgumentException($"Can't find resource with {_execOptions.ResourceName} name.");
        //}
        //if (resourceState.Resource is not IResourceSupportsExec targetExecResource)
        //{
        //    throw new ArgumentException($"Resource {_execOptions.ResourceName} does not support exec.");
        //}

        var serviceLogger = _loggerService.GetLogger(targetExecResource);
        var execLogger = new ExecLogger(serviceLogger, _logChannel);

        await targetExecResource.ExecuteAsync(_execOptions, logger: execLogger, loggerDisposable: execLogger, cancellationToken).ConfigureAwait(false);
    }
}
