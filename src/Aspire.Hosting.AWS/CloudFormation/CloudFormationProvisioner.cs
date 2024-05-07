// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Amazon.CloudFormation;
using Amazon.CloudFormation.Model;
using Amazon.Runtime;
using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.AWS.CloudFormation;
internal sealed class CloudFormationProvisioner(
    DistributedApplicationModel appModel,
    ResourceNotificationService notificationService,
    ResourceLoggerService loggerService)
{
    internal async Task ConfigureCloudFormation(CancellationToken cancellationToken = default)
    {
        await ProcessCloudFormationStackResourceAsync(cancellationToken).ConfigureAwait(false);
        await ProcessCloudFormationTemplateResourceAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task ProcessCloudFormationStackResourceAsync(CancellationToken cancellationToken = default)
    {
        foreach (var cloudFormationResource in appModel.Resources.OfType<CloudFormationStackResource>())
        {
            var logger = loggerService.GetLogger(cloudFormationResource);

            await PublishCloudFormationUpdateStateAsync(cloudFormationResource, Constants.ResourceStateStarting).ConfigureAwait(false);

            try
            {
                using var cfClient = GetCloudFormationClient(cloudFormationResource);

                var request = new DescribeStacksRequest { StackName = cloudFormationResource.Name };
                var response = await cfClient.DescribeStacksAsync(request, cancellationToken).ConfigureAwait(false);

                // If the stack didn't exist then a StackNotFoundException would have been thrown.
                var stack = response.Stacks[0];

                // Capture the CloudFormation stack output parameters on to the Aspire CloudFormation resource. This
                // allows projects that have a reference to the stack have the output parameters applied to the
                // projects IConfiguration.
                cloudFormationResource.Outputs = stack!.Outputs;

                await PublishCloudFormationUpdateStateAsync(cloudFormationResource, Constants.ResourceStateRunning, ConvertOutputToProperties(stack),
                    [
                        new UrlSnapshot("aws-console", CloudFormationAWSConsoleUrlMapper.MapCloudFormationUrl(stack.StackId), IsInternal: false)
                    ]).ConfigureAwait(false);
                await AddDashboardResources(cfClient, cloudFormationResource, cancellationToken).ConfigureAwait(false);

                cloudFormationResource.ProvisioningTaskCompletionSource?.TrySetResult();
            }
            catch (Exception e)
            {
                if (e is AmazonCloudFormationException ce && string.Equals(ce.ErrorCode, "ValidationError"))
                {
                    logger.LogError("Stack {StackName} does not exists to add as a resource.", cloudFormationResource.Name);
                }
                else
                {
                    logger.LogError(e, "Error reading {StackName}.", cloudFormationResource.Name);
                }

                await PublishCloudFormationUpdateStateAsync(cloudFormationResource, Constants.ResourceStateFailedToStart).ConfigureAwait(false);
                cloudFormationResource.ProvisioningTaskCompletionSource?.TrySetException(e);
            }
        }
    }

    private async Task ProcessCloudFormationTemplateResourceAsync(CancellationToken cancellationToken = default)
    {
        foreach (var cloudFormationResource in appModel.Resources.OfType<CloudFormationTemplateResource>())
        {
            var logger = loggerService.GetLogger(cloudFormationResource);

            try
            {
                await PublishCloudFormationUpdateStateAsync(cloudFormationResource, Constants.ResourceStateStarting).ConfigureAwait(false);

                using var cfClient = GetCloudFormationClient(cloudFormationResource);

                var executor = new CloudFormationStackExecutor(cfClient, cloudFormationResource, logger);
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

                    cloudFormationResource.Outputs = stack.Outputs;

                    await PublishCloudFormationUpdateStateAsync(cloudFormationResource, Constants.ResourceStateRunning, ConvertOutputToProperties(stack, cloudFormationResource.TemplatePath),
                        [
                            new UrlSnapshot("aws-console", CloudFormationAWSConsoleUrlMapper.MapCloudFormationUrl(stack.StackId), IsInternal: false)
                        ]).ConfigureAwait(false);
                    await AddDashboardResources(cfClient, cloudFormationResource, cancellationToken).ConfigureAwait(false);

                    cloudFormationResource.ProvisioningTaskCompletionSource?.TrySetResult();
                }
                else
                {
                    logger.LogError("CloudFormation provisioning failed");

                    await PublishCloudFormationUpdateStateAsync(cloudFormationResource, Constants.ResourceStateFailedToStart).ConfigureAwait(false);
                    cloudFormationResource.ProvisioningTaskCompletionSource?.TrySetException(new AWSProvisioningException("Failed to apply CloudFormation template", null));
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error provisioning {ResourceName} CloudFormation resource", cloudFormationResource.Name);
                await PublishCloudFormationUpdateStateAsync(cloudFormationResource, Constants.ResourceStateFailedToStart).ConfigureAwait(false);
                cloudFormationResource.ProvisioningTaskCompletionSource?.TrySetException(ex);
            }
        }
    }

    private async Task PublishCloudFormationUpdateStateAsync(CloudFormationResource resource, string status, ImmutableArray<ResourcePropertySnapshot>? properties = null, ImmutableArray<UrlSnapshot>? urls = default)
    {
        if (properties == null)
        {
            properties = ImmutableArray.Create<ResourcePropertySnapshot>();
        }

        await notificationService.PublishUpdateAsync(resource, state => state with
        {
            State = status,
            Properties = state.Properties.AddRange(properties),
            Urls = urls ?? []
        }).ConfigureAwait(false);
    }

    private static ImmutableArray<ResourcePropertySnapshot> ConvertOutputToProperties(Stack stack, string? templateFile = null)
    {
        var list = ImmutableArray.CreateBuilder<ResourcePropertySnapshot>();

        foreach (var output in stack.Outputs)
        {
            list.Add(new("aws.cloudformation.output." + output.OutputKey, output.OutputValue));
        }

        list.Add(new(CustomResourceKnownProperties.Source, stack.StackId));

        if (!string.IsNullOrEmpty(templateFile))
        {
            list.Add(new("aws.cloudformation.template", templateFile));
        }

        return list.ToImmutableArray();
    }

    private static IAmazonCloudFormation GetCloudFormationClient(ICloudFormationResource resource)
    {
        if (resource.CloudFormationClient != null)
        {
            return resource.CloudFormationClient;
        }

        try
        {
            AmazonCloudFormationClient client;
            if (resource.AWSSDKConfig != null)
            {
                var config = resource.AWSSDKConfig.CreateServiceConfig<AmazonCloudFormationConfig>();

                var awsCredentials = FallbackCredentialsFactory.GetCredentials(config);
                client = new AmazonCloudFormationClient(awsCredentials, config);
            }
            else
            {
                client = new AmazonCloudFormationClient();
            }

            client.BeforeRequestEvent += SdkUtilities.ConfigureUserAgentString;

            return client;
        }
        catch (Exception e)
        {
            throw new AWSProvisioningException("Failed to construct AWS CloudFormation service client to provision AWS resources.", e);
        }
    }

    private async Task AddDashboardResources(IAmazonCloudFormation cfClient, CloudFormationResource cloudFormationResource, CancellationToken cancellationToken = default)
    {
        var logger = loggerService.GetLogger(cloudFormationResource);

        try
        {
            var request = new DescribeStackResourcesRequest { StackName = cloudFormationResource.Name };
            var response = await cfClient.DescribeStackResourcesAsync(request, cancellationToken).ConfigureAwait(false);

            foreach (var stackResource in response.StackResources)
            {
                var url = CloudFormationAWSConsoleUrlMapper.MapResourceUrl(stackResource.StackId, stackResource.ResourceType, stackResource.PhysicalResourceId);

                if (url == null)
                {
                    continue;
                }

                var resourceName = $"{cloudFormationResource.Name}-{stackResource.LogicalResourceId}";

                if (resourceName.Length > 64)
                {
                    resourceName = resourceName[..64];
                }

                var resource = new CloudFormationPhysicalResource(resourceName);

                await notificationService.PublishUpdateAsync(resource,
                        state => state with
                        {
                            State = Constants.ResourceStateRunning,
                            Urls = [new UrlSnapshot("aws-console", url, IsInternal: false)],
                            ResourceType = stackResource.ResourceType,
                            Properties = [new ResourcePropertySnapshot(CustomResourceKnownProperties.Source, stackResource.PhysicalResourceId)]
                        }).ConfigureAwait(false);
            }
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error reading resources from {StackName}.", cloudFormationResource.Name);
        }
    }
}
