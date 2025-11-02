// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREPIPELINES001
#pragma warning disable ASPIREPIPELINES003
#pragma warning disable ASPIRECONTAINERRUNTIME001

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure.Provisioning.Internal;
using Aspire.Hosting.Pipelines;
using Aspire.Hosting.Publishing;
using Azure.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.Azure;

/// <summary>
/// Helper methods for Azure environment resources to handle container image operations.
/// </summary>
internal static class AzureEnvironmentResourceHelpers
{
    private const string AcrUsername = "00000000-0000-0000-0000-000000000000";
    private const string AcrScope = "https://containerregistry.azure.net/.default";

    public static async Task LoginToRegistryAsync(IContainerRegistry registry, PipelineStepContext context)
    {
        var containerRuntime = context.Services.GetRequiredService<IContainerRuntime>();
        var tokenCredentialProvider = context.Services.GetRequiredService<ITokenCredentialProvider>();

        var registryName = await registry.Name.GetValueAsync(context.CancellationToken).ConfigureAwait(false) ??
                         throw new InvalidOperationException("Failed to retrieve container registry information.");
        
        var registryEndpoint = await registry.Endpoint.GetValueAsync(context.CancellationToken).ConfigureAwait(false) ??
                              throw new InvalidOperationException("Failed to retrieve container registry endpoint.");

        var loginTask = await context.ReportingStep.CreateTaskAsync($"Logging in to **{registryName}**", context.CancellationToken).ConfigureAwait(false);
        await using (loginTask.ConfigureAwait(false))
        {
            await AuthenticateToAcrHelper(loginTask, registryEndpoint, containerRuntime, tokenCredentialProvider.TokenCredential, context.Logger, context.CancellationToken).ConfigureAwait(false);
        }
    }

    private static async Task AuthenticateToAcrHelper(IReportingTask loginTask, string registryEndpoint, IContainerRuntime containerRuntime, TokenCredential credential, ILogger logger, CancellationToken cancellationToken)
    {
        try
        {
            // Acquire access token for Azure Container Registry
            var tokenRequestContext = new TokenRequestContext([AcrScope]);
            var accessToken = await credential.GetTokenAsync(tokenRequestContext, cancellationToken).ConfigureAwait(false);

            logger.LogDebug("Logging in to registry {RegistryEndpoint} using container runtime {RuntimeName}", registryEndpoint, containerRuntime.Name);

            // Login to the registry using the container runtime
            await containerRuntime.LoginToRegistryAsync(registryEndpoint, AcrUsername, accessToken.Token, cancellationToken).ConfigureAwait(false);

            await loginTask.CompleteAsync($"Successfully logged in to **{registryEndpoint}**", CompletionState.Completed, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            await loginTask.FailAsync($"Login to ACR **{registryEndpoint}** failed: {ex.Message}", cancellationToken: cancellationToken).ConfigureAwait(false);
            throw;
        }
    }

    public static async Task PushImageToRegistryAsync(IContainerRegistry registry, IResource resource, PipelineStepContext context, IResourceContainerImageBuilder containerImageBuilder)
    {
        var registryName = await registry.Name.GetValueAsync(context.CancellationToken).ConfigureAwait(false) ??
                         throw new InvalidOperationException("Failed to retrieve container registry information.");

        if (!resource.TryGetContainerImageName(out var localImageName))
        {
            localImageName = resource.Name.ToLowerInvariant();
        }

        IValueProvider cir = new ContainerImageReference(resource);
        var targetTag = await cir.GetValueAsync(context.CancellationToken).ConfigureAwait(false);

        var pushTask = await context.ReportingStep.CreateTaskAsync($"Pushing **{resource.Name}** to **{registryName}**", context.CancellationToken).ConfigureAwait(false);
        await using (pushTask.ConfigureAwait(false))
        {
            try
            {
                if (targetTag == null)
                {
                    throw new InvalidOperationException($"Failed to get target tag for {resource.Name}");
                }
                await TagAndPushImage(localImageName, targetTag, context.CancellationToken, containerImageBuilder).ConfigureAwait(false);
                await pushTask.CompleteAsync($"Successfully pushed **{resource.Name}** to `{targetTag}`", CompletionState.Completed, context.CancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await pushTask.CompleteAsync($"Failed to push **{resource.Name}**: {ex.Message}", CompletionState.CompletedWithError, context.CancellationToken).ConfigureAwait(false);
                throw;
            }
        }
    }

    private static async Task TagAndPushImage(string localTag, string targetTag, CancellationToken cancellationToken, IResourceContainerImageBuilder containerImageBuilder)
    {
        await containerImageBuilder.TagImageAsync(localTag, targetTag, cancellationToken).ConfigureAwait(false);
        await containerImageBuilder.PushImageAsync(targetTag, cancellationToken).ConfigureAwait(false);
    }
}
