// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Amazon.CloudFormation;
using Amazon.Runtime;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.AWS.CloudFormation;
using Aspire.Hosting.Lifecycle;
using Microsoft.Extensions.Logging;
using CloudFormationStack = Amazon.CloudFormation.Model.Stack;

namespace Aspire.Hosting.AWS.CDK;

internal sealed class CDKProvisioner(
    CDKApplicationExecutionContext cdkContext,
    DistributedApplicationExecutionContext executionContext,
    ResourceNotificationService notificationService,
    ResourceLoggerService loggerService) : IDistributedApplicationLifecycleHook
{

    public async Task BeforeStartAsync(DistributedApplicationModel appModel, CancellationToken cancellationToken = default)
    {
        var context = new CDKProvisionerContext(appModel, notificationService);
        if (executionContext.IsPublishMode)
        {
            SynthesizeStackResources(appModel);
            return;
        }

        foreach (var stackResource in appModel.Resources.OfType<StackResource>())
        {
            await context.PublishUpdateStateAsync(stackResource, Constants.ResourceStateStarting).ConfigureAwait(false);
            stackResource.ProvisioningTaskCompletionSource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        _ = Task.Run(() => ProvisionStackResources(appModel, cancellationToken), cancellationToken);
    }

    private void SynthesizeStackResources(DistributedApplicationModel appModel)
    {
        var context = new CDKProvisionerContext(appModel, notificationService);
        SynthesizeStackResourcesInternal(context);
    }

    private async Task ProvisionStackResources(DistributedApplicationModel appModel, CancellationToken cancellationToken = default)
    {
        var context = new CDKProvisionerContext(appModel, notificationService);
        var templates = SynthesizeStackResourcesInternal(context);

        await DeployCDKStackTemplatesAsync(templates, context, cancellationToken).ConfigureAwait(false);
    }

    private IEnumerable<CDKStackTemplate> SynthesizeStackResourcesInternal(CDKProvisionerContext context)
    {
        ModifyResourcesWithConstructs(context);
        var cloudAssembly = cdkContext.App.Synth();
        // Guard when stack contains assets
        foreach (var stack in cloudAssembly.Stacks)
        {
            var stackResource = context.StackResources.Single(s => s.Stack.StackName == stack.StackName);
            if (stack.Assets.Length != 0)
            {
                var logger = loggerService.GetLogger(stackResource);
                logger.LogError("CDK stack {StackResourceName} contains assets and is currently not supported", stackResource.Name);
                context.PublishUpdateStateAsync(stackResource, Constants.ResourceStateFailedToStart).Wait();
            }
            yield return new CDKStackTemplate(stack, context.StackResources.Single(s => s.Stack.StackName == stack.StackName));
        }
    }

    private static void ModifyResourcesWithConstructs(CDKProvisionerContext context)
    {
        // Modified constructs after build
        foreach (var constructResource in context.AppModel.Resources.OfType<IResourceWithConstruct>())
        {
            // Find Construct Modifier Annotations
            if (!constructResource.TryGetAnnotationsOfType<IConstructModifierAnnotation>(out var modifiers))
            {
                continue;
            }

            // Modify stack
            foreach (var modifier in modifiers)
            {
                modifier.ChangeConstruct(constructResource.Construct);
            }
        }
    }

    private async Task DeployCDKStackTemplatesAsync(IEnumerable<CDKStackTemplate> templates, CDKProvisionerContext context, CancellationToken cancellationToken = default)
    {
        foreach (var template in templates)
        {
            var logger = loggerService.GetLogger(template.Resource);

            try
            {
                await context.PublishUpdateStateAsync(template.Resource, Constants.ResourceStateStarting).ConfigureAwait(false);

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

                    template.Resource.Outputs = stack.Outputs;
                    await context.PublishUpdateStateAsync(template.Resource, Constants.ResourceStateRunning, ConvertOutputToProperties(stack, template.Artifact.TemplateFullPath)).ConfigureAwait(false);
                    template.Resource.ProvisioningTaskCompletionSource?.TrySetResult();
                }
                else
                {
                    logger.LogError("CDK provisioning failed");

                    await context.PublishUpdateStateAsync(template.Resource, Constants.ResourceStateFailedToStart).ConfigureAwait(false);
                    template.Resource.ProvisioningTaskCompletionSource?.TrySetException(new AWSProvisioningException("Failed to apply CloudFormation template", null));
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error provisioning {ResourceName} CDK resource", template.Resource.Name);
                await context.PublishUpdateStateAsync(template.Resource, Constants.ResourceStateFailedToStart).ConfigureAwait(false);
                template.Resource.ProvisioningTaskCompletionSource?.TrySetException(ex);
            }
        }
    }

    private static ImmutableArray<ResourcePropertySnapshot> ConvertOutputToProperties(CloudFormationStack stack, string? templateFile = null)
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
}
