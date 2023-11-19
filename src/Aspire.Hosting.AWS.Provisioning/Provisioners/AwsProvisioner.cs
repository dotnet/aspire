// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Amazon.CloudFormation;
using Amazon.CloudFormation.Model;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.AWS.CloudFormation;
using Aspire.Hosting.AWS.Provisioning;
using Aspire.Hosting.AWS.Provisioning.Provisioners;
using Aspire.Hosting.Lifecycle;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using InvalidOperationException = System.InvalidOperationException;

namespace Aspire.Hosting.Azure;

// Provisions aws resources for development purposes
internal sealed class AwsProvisioner(
    IAmazonCloudFormation cloudFormationClient,
    IServiceProvider serviceProvider,
    // IHostEnvironment environment,
    IConfiguration configuration,
    IOptions<AwsProvisionerOptions> options,
    ILogger<AwsProvisioner> logger) : IDistributedApplicationLifecycleHook
{
    private static readonly JsonSerializerOptions s_cloudFormationJsonSerializerOptions = new() { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };

    private readonly AwsProvisionerOptions _options = options.Value;

    public async Task BeforeStartAsync(DistributedApplicationModel appModel, CancellationToken cancellationToken = default)
    {
        var awsResources = appModel.Resources.OfType<IAwsResource>().ToList();
        if (!awsResources.Any())
        {
            return;
        }

        // TODO: run the container somewhere here

        try
        {
            await ProvisionAwsResources(awsResources, cancellationToken).ConfigureAwait(false);
        }
        catch (MissingConfigurationException ex)
        {
            logger.LogWarning(ex, "Required configuration is missing");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error provisioning Aws resources");
        }
    }

    private async Task ProvisionAwsResources(IEnumerable<IAwsResource> awsResources, CancellationToken cancellationToken)
    {
        // AWS SDK searches for credentials in the following sources:
        // - Environment variables
        // - Shared credentials file
        // - AWS credentials profile
        // If no credentials are found, the SDK will throw an exception.

        var stackName = _options.StackName ??
                        throw new MissingConfigurationException("A cloudformation stack name is required. Set the AWS:StackName configuration value.");

        // Now we have a list of resources to provision in a cloud formation template
        var cloudFormationTemplate = ProcessResources(awsResources);

        // TODO: Use JsonSerializer source generators
        var templateBody = JsonSerializer.Serialize(cloudFormationTemplate, s_cloudFormationJsonSerializerOptions);
        var isTemplateValid = await IsTemplateValidAsync(templateBody).ConfigureAwait(false);

        if (!isTemplateValid)
        {
            // TODO: Maybe throw a custom exception here?
            throw new InvalidOperationException("CloudFormation template is not valid");
        }

        var stackStatus = await CheckIfStackExists(stackName, cancellationToken).ConfigureAwait(false);

        if (stackStatus is null)
        {
            await CreateStackAsync(stackName, templateBody, cancellationToken).ConfigureAwait(false);
        }
        else if (stackStatus == StackStatus.CREATE_COMPLETE || stackStatus == StackStatus.UPDATE_COMPLETE)
        {
            await UpdateStackAsync(stackName, templateBody, cancellationToken).ConfigureAwait(false);
        }
        else if (stackStatus == StackStatus.CREATE_FAILED || stackStatus == StackStatus.ROLLBACK_FAILED || stackStatus == StackStatus.UPDATE_ROLLBACK_FAILED)
        {
            // The stack is in a failed state, so delete it before creating a new one

            await DeleteStackAsync(stackName, cancellationToken).ConfigureAwait(false);
            await CreateStackAsync(stackName, templateBody, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            logger.LogInformation("Stack {StackName} is in {StackStatus} state. No action required", stackName, stackStatus);
        }

        // TODO: add cloud formation outputs to the configure aws resources
    }

    private CloudFormationTemplate ProcessResources(IEnumerable<IAwsResource> awsResources)
    {
        var usedResources = new HashSet<string>();
        var cloudFormationTemplate = new CloudFormationTemplate();

        var resources = awsResources.ToList();
        foreach (var resource in resources)
        {
            if (usedResources.Contains(resource.Name))
            {
                continue;
            }

            var provisioner = serviceProvider.GetKeyedService<IAwsResourceProvisioner>(resource.GetType());

            if (provisioner is null)
            {
                logger.LogWarning("No provisioner found for {ResourceType} skipping", resource.GetType().Name);
                continue;
            }

            provisioner.ConfigureResource(configuration, resource);

            // TODO: We're keeping state in the provisioners, which is not ideal.
            // Maybe passing all resource to provisioners and do operations below in the provisioner itself?
            // We can utilize ProvisioningContext for this purpose.
            if (resource is AwsSnsTopicResource topicResource && topicResource.Subscriptions.Any())
            {
                // TODO: null check
                var snsProvisioner = (SnsProvisioner)provisioner;
                try
                {
                    var snsSubscribersResources = topicResource.Subscriptions
                        .Select(resName => resources.Single(awsResource => awsResource.Name == resName));

                    foreach (var snsSubscriberResource in snsSubscribersResources)
                    {
                        var subProvisioner = serviceProvider.GetKeyedService<IAwsResourceProvisioner>(snsSubscriberResource.GetType());

                        subProvisioner!.ConfigureResource(configuration, snsSubscriberResource);

                        var awsConstruct = subProvisioner.CreateConstruct(snsSubscriberResource, new ProvisioningContext());
                        snsProvisioner.AddSubscriptions(awsConstruct);

                        cloudFormationTemplate.AddResource(awsConstruct);
                        usedResources.Add(snsSubscriberResource.Name);
                    }
                }
                catch (InvalidOperationException e)
                {
                    throw new MissingConfigurationException($"The resource {resource.Name} has a subscription to a resource that does not exist.", e);
                }
            }

            // TODO: Couldn't figure out if need ProvisioningContext yet, so I'm keeping it here for now
            var construct = provisioner.CreateConstruct(resource, new ProvisioningContext());
            cloudFormationTemplate.AddResource(construct);
            usedResources.Add(resource.Name);
        }

        return cloudFormationTemplate;
    }

    private async Task CreateStackAsync(string stackName, string templateBody, CancellationToken cancellationToken)
    {
        logger.LogInformation("Creating stack {StackName}", stackName);

        var createStackRequest = new CreateStackRequest { StackName = stackName, TemplateBody = templateBody };

        // TODO: Handle exceptions
        var createStackResponse = await cloudFormationClient.CreateStackAsync(createStackRequest, cancellationToken).ConfigureAwait(false);

        if(createStackResponse.HttpStatusCode != HttpStatusCode.OK)
        {
            logger.LogError("Error creating stack {StackName}", stackName);

            // TODO: throw custom exception
        }

        var desiredStatusesForCreation = new HashSet<StackStatus> { StackStatus.CREATE_COMPLETE, StackStatus.ROLLBACK_COMPLETE };
        // I'm not sure if timeout should be configurable
        await WaitForStackCompletion(stackName, desiredStatusesForCreation, TimeSpan.FromMinutes(10), cancellationToken).ConfigureAwait(false);
    }

    private async Task UpdateStackAsync(string stackName, string templateBody, CancellationToken cancellationToken)
    {
        logger.LogInformation("Updating stack {StackName}", stackName);

        var updateStackRequest = new UpdateStackRequest { StackName = stackName, TemplateBody = templateBody };

        try
        {
            var updateStackResponse = await cloudFormationClient.UpdateStackAsync(updateStackRequest, cancellationToken).ConfigureAwait(false);

            if (updateStackResponse.HttpStatusCode != HttpStatusCode.OK)
            {
                logger.LogError("Error updating stack {StackName}", stackName);

                // TODO: throw custom exception
            }
        }
        catch (AmazonCloudFormationException e)
        {
            if (!(e.ErrorCode == "ValidationError" && e.Message.Contains("No updates are to be performed.")))
            {
                // We don't want to throw if there are no updates to be performed
                logger.LogWarning(e, "Error updating stack {StackName}", stackName);
            }
        }

        var desiredStatusesForUpdate = new HashSet<StackStatus> { StackStatus.UPDATE_COMPLETE, StackStatus.UPDATE_ROLLBACK_COMPLETE };
        await WaitForStackCompletion(stackName, desiredStatusesForUpdate, TimeSpan.FromMinutes(10), cancellationToken).ConfigureAwait(false);
    }

    private async Task DeleteStackAsync(string stackName, CancellationToken cancellationToken)
    {
        var deleteStackRequest = new DeleteStackRequest { StackName = stackName };
        var deleteStackResponse = await cloudFormationClient.DeleteStackAsync(deleteStackRequest, cancellationToken).ConfigureAwait(false);

        if (deleteStackResponse.HttpStatusCode != HttpStatusCode.OK)
        {
            logger.LogError("Error deleting stack {StackName}", stackName);

            // TODO: throw custom exception
        }

        var desiredStatusesForDelete= new HashSet<StackStatus> { StackStatus.UPDATE_COMPLETE, StackStatus.UPDATE_ROLLBACK_COMPLETE };
        await WaitForStackCompletion(stackName, desiredStatusesForDelete, TimeSpan.FromMinutes(10), cancellationToken).ConfigureAwait(false);
    }

    private async Task<StackStatus?> CheckIfStackExists(string stackName, CancellationToken cancellationToken)
    {
        try
        {
            var describeStacksRequest = new DescribeStacksRequest { StackName = stackName };
            var describeStacksResponse = await cloudFormationClient.DescribeStacksAsync(describeStacksRequest, cancellationToken).ConfigureAwait(false);
            var stackStatus = describeStacksResponse.Stacks.FirstOrDefault()?.StackStatus;

            return stackStatus;
        }
        catch (AmazonCloudFormationException e)
        {
            if (!(e.ErrorCode == "ValidationError" && e.Message.Contains("does not exist")))
            {
                throw;
            }

            return null;
        }
    }

    private async Task<bool> IsTemplateValidAsync(string templateBody)
    {
        try
        {
            var validateRequest = new ValidateTemplateRequest { TemplateBody = templateBody };
            var response = await cloudFormationClient.ValidateTemplateAsync(validateRequest).ConfigureAwait(false);

            logger.LogInformation("Template validation succeeded for {TemplateBody}", templateBody);

            // Optionally, print additional details about the template
            foreach (var parameter in response.Parameters)
            {
                logger.LogInformation("Parameter: {ParameterParameterKey}, Default: {ParameterDefaultValue}", parameter.ParameterKey, parameter.DefaultValue);
            }

            return true;
        }
        catch (AmazonCloudFormationException ex)
        {
            logger.LogError(ex, "Template validation failed for {TemplateBody}", templateBody);
            return false;
        }
    }

    private async Task WaitForStackCompletion(string stackName, IReadOnlySet<StackStatus> desiredStatuses, TimeSpan timeout, CancellationToken cancellationToken)
    {
        var startTime = DateTime.UtcNow;

        logger.LogInformation("Waiting for stack {StackName} to reach the desired state...", stackName);

        while (DateTime.UtcNow - startTime < timeout)
        {
            logger.LogInformation("Checking stack {StackName} status...", stackName);

            var response = await cloudFormationClient.DescribeStacksAsync(new DescribeStacksRequest { StackName = stackName }, cancellationToken).ConfigureAwait(false);

            // Can it be multiple stacks with the same name?
            var currentStatus = response.Stacks.FirstOrDefault()?.StackStatus;

            logger.LogInformation("Stack {StackName} is in {StackStatus} state", stackName, currentStatus);

            // Not sure if this is the best way to check if the stack is in the desired state
            // TODO: Null check
            if (desiredStatuses.Contains(currentStatus!))
            {
                return; // Desired state reached
            }

            logger.LogInformation("Stack {StackName} is not in the desired state yet. Waiting for 30 secs...", stackName);
            await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken).ConfigureAwait(false); // Polling interval
        }

        throw new TimeoutException("Timeout waiting for stack to reach the desired state.");
    }

    private sealed class MissingConfigurationException : Exception
    {
        public MissingConfigurationException(string message) : base(message)
        {
        }

        public MissingConfigurationException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
