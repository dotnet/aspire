// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.AWS.CloudFormation;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.AWS.Provisioning;

internal class CloudFormationTemplateResourceProvisioner(
    ResourceLoggerService loggerService,
    ResourceNotificationService notificationService)
    : CloudFormationTemplateResourceProvisioner<CloudFormationTemplateResource>(loggerService, notificationService);

internal class CloudFormationTemplateResourceProvisioner<T>(
    ResourceLoggerService loggerService,
    ResourceNotificationService notificationService)
    : CloudFormationResourceProvisioner<T>(loggerService, notificationService)
    where T : CloudFormationTemplateResource
{
    protected override async Task GetOrCreateResourceAsync(T resource,
        CancellationToken cancellationToken)
    {
        var logger = LoggerService.GetLogger(resource);

        using var cfClient = GetCloudFormationClient(resource);

        try
        {
            var context = await CreateCloudFormationExecutionContextAsync(resource, cancellationToken)
                .ConfigureAwait(false);
            var executor = new CloudFormationStackExecutor(cfClient, context, logger);
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

                if (resource is CloudFormationResource cloudformationResource)
                {
                    cloudformationResource.Outputs = stack.Outputs;
                }

                var templatePath = resource.TemplatePath;
                await PublishCloudFormationUpdatePropertiesAsync(resource, ConvertOutputToProperties(stack, templatePath),
                    MapCloudFormationStackUrl(cfClient, stack.StackId)).ConfigureAwait(false);
            }
            else
            {
                logger.LogError("CloudFormation provisioning failed");

                throw new AWSProvisioningException("Failed to apply CloudFormation template");
            }
        }
        catch (Exception ex)
        {
            HandleTemplateProvisioningException(ex, resource, logger);
            throw;
        }
    }

    private static async Task<CloudFormationStackExecutionContext> CreateCloudFormationExecutionContextAsync(T resource, CancellationToken cancellationToken)
    {
        var template = await File.ReadAllTextAsync(resource.TemplatePath, cancellationToken).ConfigureAwait(false);
        return new CloudFormationStackExecutionContext(resource.StackName, template)
        {
            RoleArn = resource.RoleArn,
            DisableDiffCheck = resource.DisableDiffCheck,
            StackPollingInterval = resource.StackPollingInterval,
            DisabledCapabilities = resource.DisabledCapabilities,
            CloudFormationParameters = resource.CloudFormationParameters
        };
    }

    protected virtual void HandleTemplateProvisioningException(Exception ex, T resource, ILogger logger) { }
}
