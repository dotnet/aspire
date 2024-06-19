// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.AWS.CloudFormation;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.AWS.Provisioning;

internal sealed class CloudFormationTemplateResourceProvisioner(
    ResourceLoggerService loggerService,
    ResourceNotificationService notificationService)
    : CloudFormationResourceProvisioner<CloudFormationTemplateResource>(notificationService)
{
    protected override async Task GetOrCreateResourceAsync(CloudFormationTemplateResource resource, CancellationToken cancellationToken)
    {
        var logger = loggerService.GetLogger(resource);

        using var cfClient = GetCloudFormationClient(resource);

        var executor = new CloudFormationStackExecutor(cfClient, resource, logger);
        var stack = await executor.ExecuteTemplateAsync(cancellationToken).ConfigureAwait(false);

        if (stack != null)
        {
            logger.LogInformation("CloudFormation stack has {Count} output parameters", stack.Outputs.Count);
            if (logger.IsEnabled(LogLevel.Information))
            {
                foreach (var output in stack.Outputs)
                {
                    logger.LogInformation("Output Name: {Name}, Value {Value}", output.OutputKey, output.OutputValue);
                }
            }

            logger.LogInformation("CloudFormation provisioning complete");

            resource.Outputs = stack.Outputs;
            await PublishCloudFormationUpdateStateAsync(resource, Constants.ResourceStateRunning, ConvertOutputToProperties(stack, resource.TemplatePath)).ConfigureAwait(false);
        }
        else
        {
            logger.LogError("CloudFormation provisioning failed");

            throw new AWSProvisioningException("Failed to apply CloudFormation template", null);
        }
    }
}
