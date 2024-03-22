// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Amazon.CDK;
using Amazon.CloudFormation;
using Amazon.Runtime;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.AWS.CloudFormation;
using Microsoft.Extensions.Logging;
using Stack = Amazon.CDK.Stack;
using CloudFormationStack = Amazon.CloudFormation.Model.Stack;

namespace Aspire.Hosting.AWS.CDK;

internal sealed class AWSCDKProvisioner(
    DistributedApplicationModel appModel,
    ResourceNotificationService notificationService,
    ResourceLoggerService loggerService)
{
    internal async Task ProvisionCloudFormation(CancellationToken cancellationToken = default)
    {
        var templates = ProcessCDKStackResources();
        await DeployCDKStackTemplatesAsync(templates, cancellationToken).ConfigureAwait(false);
    }

    private IEnumerable<AWSCDKStackTemplate> ProcessCDKStackResources()
    {
        var app = new App();
        var stackResources = appModel.Resources.OfType<StackResource>().ToDictionary(resource => resource.Name);
        // Build Stacks
        foreach (var stackResource in stackResources.Values)
        {
            stackResource.BuildStack(app);
        }
        // Modified Stacks after build
        foreach (var stackResource in stackResources.Values)
        {
            // Find Stack Modifier Annotations
            if (!stackResource.TryGetAnnotationsOfType<IStackModifierAnnotation>(out var modifiers))
            {
                continue;
            }
            // Find Stack Nodes
            if (app.Node.TryFindChild(stackResource.Name) is not Stack stack)
            {
                continue;
            }

            // Modify stack
            foreach (var modifier in modifiers)
            {
                modifier.ChangeStack(stack);
            }
        }

        var cloudAssembly = app.Synth();
        return cloudAssembly.Stacks.Select(stack => new AWSCDKStackTemplate(stack, stackResources[stack.StackName]));
    }

    private async Task DeployCDKStackTemplatesAsync(IEnumerable<AWSCDKStackTemplate> templates, CancellationToken cancellationToken = default)
    {
        foreach (var template in templates)
        {
            var logger = loggerService.GetLogger(template.Resource);

            try
            {
                await PublishCloudFormationUpdateStateAsync(template.Resource, Constants.ResourceStateStarting).ConfigureAwait(false);

                using var cfClient = GetCloudFormationClient(template.Resource);

                var executor = new CloudFormationStackExecutor(cfClient, template, logger);
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

                    template.Resource.Outputs = stack.Outputs;
                    await PublishCloudFormationUpdateStateAsync(template.Resource, Constants.ResourceStateRunning, ConvertOutputToProperties(stack, template.Artifact.TemplateFullPath)).ConfigureAwait(false);
                    template.Resource.ProvisioningTaskCompletionSource?.TrySetResult();
                }
                else
                {
                    logger.LogError("CloudFormation provisioning failed");

                    await PublishCloudFormationUpdateStateAsync(template.Resource, Constants.ResourceStateFailedToStart).ConfigureAwait(false);
                    template.Resource.ProvisioningTaskCompletionSource?.TrySetException(new AWSProvisioningException("Failed to apply CloudFormation template", null));
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error provisioning {ResourceName} CloudFormation resource", template.Resource.Name);
                await PublishCloudFormationUpdateStateAsync(template.Resource, Constants.ResourceStateFailedToStart).ConfigureAwait(false);
                template.Resource.ProvisioningTaskCompletionSource?.TrySetException(ex);
            }
        }
    }

    private async Task PublishCloudFormationUpdateStateAsync(CloudFormationResource resource, string status, ImmutableArray<(string, object?)>? properties = null)
    {
        if (properties == null)
        {
            properties = ImmutableArray.Create<(string, object?)>();
        }

        await notificationService.PublishUpdateAsync(resource, state => state with
        {
            State = status,
            Properties = state.Properties.AddRange(properties)
        }).ConfigureAwait(false);
    }

    private static ImmutableArray<(string, object?)> ConvertOutputToProperties(CloudFormationStack stack, string? templateFile = null)
    {
        var list = stack.Outputs.Select(output => ("aws.cloudformation.output." + output.OutputKey, output.OutputValue)).Select(dummy => ((string, object?))dummy).ToList();

        list.Add((CustomResourceKnownProperties.Source, stack.StackId));

        if (!string.IsNullOrEmpty(templateFile))
        {
            list.Add(("aws.cloudformation.template", templateFile));
        }

        return ImmutableArray.Create(list.ToArray());
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
