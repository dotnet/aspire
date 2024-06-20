// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.AWS.CDK;
using Aspire.Hosting.AWS.Provisioning.Provisioners;

namespace Aspire.Hosting.AWS.Provisioning;

internal sealed class CDKStackResourceProvisioner<T>(
    ResourceLoggerService loggerService,
    ResourceNotificationService notificationService)
    : CloudFormationResourceProvisioner<T>(loggerService, notificationService)
    where T : IStackResource
{
    protected override Task GetOrCreateResourceAsync(T resource, ProvisioningContext context, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    /*private Task ProvisionCDKStackAssetsAsync(CDKStackTemplate template)
    {
        var logger = loggerService.GetLogger(template.Resource);
        if (template.Artifact.Dependencies
            .OfType<AssetManifestArtifact>()
            .Any(dependency =>
                dependency.Contents.Files?.Count > 1
                || dependency.Contents.DockerImages?.Count > 0))
        {
            logger.LogError("File or container image assets are currently not supported");
            throw new AWSProvisioningException("Failed to provision stack assets. Provisioning file or container image assets are currently not supported.");
        }

        return Task.CompletedTask;
    }

    private async Task<bool> ProvisionCDKStackTemplatesAsync(CDKStackTemplate template, CancellationToken cancellationToken = default)
    {
        var logger = LoggerService.GetLogger(template.Resource);
        try
        {
            using var cfClient = GetCloudFormationClient(template.Resource);

            var executor = new CloudFormationStackExecutor(cfClient, template, logger);
            var stack = await executor.ExecuteTemplateAsync(cancellationToken).ConfigureAwait(false);

            if (stack != null)
            {
                logger.LogInformation("CDK stack has {Count} output parameters", stack.Outputs.Count);
                if (logger.IsEnabled(LogLevel.Information))
                {
                    foreach (var output in stack.Outputs)
                    {
                        logger.LogInformation("Output Name: {Name}, Value {Value}", output.OutputKey, output.OutputValue);
                    }
                }

                logger.LogInformation("CDK provisioning complete");

                if (template.Resource is CloudFormationResource cloudFormationResource)
                {
                    cloudFormationResource.Outputs = stack.Outputs;
                }
                //await PublishUpdateStateAsync(template.Resource, Constants.ResourceStateRunning, ConvertOutputToProperties(stack, template.Artifact.TemplateFullPath)).ConfigureAwait(false);
                template.Resource.ProvisioningTaskCompletionSource?.TrySetResult();
                return true;
            }
            logger.LogError("CDK provisioning failed");

            //await PublishUpdateStateAsync(template.Resource, Constants.ResourceStateFailedToStart).ConfigureAwait(false);
            template.Resource.ProvisioningTaskCompletionSource?.TrySetException(new AWSProvisioningException("Failed to apply CloudFormation template"));
        }
        catch (Exception ex)
        {
            if (ex.InnerException is AmazonCloudFormationException inner && inner.Message.StartsWith(@"Unable to fetch parameters [/cdk-bootstrap/"))
            {
                logger.LogError("The environment doesn't have the CDK toolkit stack installed. Use 'cdk boostrap' to setup your environment for use AWS CDK with Aspire");
            }
            else
            {
                logger.LogError(ex, "Error provisioning {ResourceName} CDK resource", template.Resource.Name);
            }
            //await PublishUpdateStateAsync(template.Resource, Constants.ResourceStateFailedToStart).ConfigureAwait(false);
            template.Resource.ProvisioningTaskCompletionSource?.TrySetException(ex);
        }
        return false;
    }*/
    protected override Task<CloudFormationStackExecutionContext> CreateCloudFormationExecutionContext(T resource, CancellationToken cancellationToken)
    {
        var artifact = resource.Annotations.OfType<StackArtifactResourceAnnotation>().Single();
        var template = JsonSerializer.Serialize(artifact.StackArtifact.Template);
        return Task.FromResult(new CloudFormationStackExecutionContext(resource.Name, template));
    }
}
