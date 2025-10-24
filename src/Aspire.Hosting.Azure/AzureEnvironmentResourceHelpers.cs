// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIRECOMPUTE001
#pragma warning disable ASPIREPUBLISHERS001

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure.Provisioning.Internal;
using Aspire.Hosting.Dcp.Process;
using Aspire.Hosting.Pipelines;
using Aspire.Hosting.Publishing;
using Microsoft.Extensions.Configuration;

namespace Aspire.Hosting.Azure;

/// <summary>
/// Helper methods for Azure environment resources to handle container image operations.
/// </summary>
internal static class AzureEnvironmentResourceHelpers
{
    public static async Task LoginToRegistryAsync(IContainerRegistry registry, PipelineStepContext context, IProcessRunner processRunner, IConfiguration configuration)
    {
        var registryName = await registry.Name.GetValueAsync(context.CancellationToken).ConfigureAwait(false) ??
                         throw new InvalidOperationException("Failed to retrieve container registry information.");

        var loginTask = await context.ReportingStep.CreateTaskAsync($"Logging in to **{registryName}**", context.CancellationToken).ConfigureAwait(false);
        await using (loginTask.ConfigureAwait(false))
        {
            await AuthenticateToAcrHelper(loginTask, registryName, context.CancellationToken, processRunner, configuration).ConfigureAwait(false);
        }
    }

    private static async Task AuthenticateToAcrHelper(IReportingTask loginTask, string registryName, CancellationToken cancellationToken, IProcessRunner processRunner, IConfiguration configuration)
    {
        var command = BicepCliCompiler.FindFullPathFromPath("az") ?? throw new InvalidOperationException("Failed to find 'az' command");
        try
        {
            var loginSpec = new ProcessSpec(command)
            {
                Arguments = $"acr login --name {registryName}",
                ThrowOnNonZeroReturnCode = false
            };

            // Set DOCKER_COMMAND environment variable if using podman
            var containerRuntime = GetContainerRuntime(configuration);
            if (string.Equals(containerRuntime, "podman", StringComparison.OrdinalIgnoreCase))
            {
                loginSpec.EnvironmentVariables["DOCKER_COMMAND"] = "podman";
            }

            var (pendingResult, processDisposable) = processRunner.Run(loginSpec);
            await using (processDisposable.ConfigureAwait(false))
            {
                var result = await pendingResult.WaitAsync(cancellationToken).ConfigureAwait(false);

                if (result.ExitCode != 0)
                {
                    await loginTask.FailAsync($"Login to ACR **{registryName}** failed with exit code {result.ExitCode}", cancellationToken: cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    await loginTask.CompleteAsync($"Successfully logged in to **{registryName}**", CompletionState.Completed, cancellationToken).ConfigureAwait(false);
                }
            }
        }
        catch (Exception)
        {
            throw;
        }
    }

    private static string? GetContainerRuntime(IConfiguration configuration)
    {
        // Fall back to known config names (primary and legacy)
        return configuration["ASPIRE_CONTAINER_RUNTIME"] ?? configuration["DOTNET_ASPIRE_CONTAINER_RUNTIME"];
    }

    public static async Task PushImagesToRegistryAsync(IContainerRegistry registry, List<IResource> resources, PipelineStepContext context, IResourceContainerImageBuilder containerImageBuilder)
    {
        var registryName = await registry.Name.GetValueAsync(context.CancellationToken).ConfigureAwait(false) ??
                         throw new InvalidOperationException("Failed to retrieve container registry information.");

        var resourcePushTasks = resources
            .Where(r => r.RequiresImageBuildAndPush())
            .Select(async resource =>
            {
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
            });

        await Task.WhenAll(resourcePushTasks).ConfigureAwait(false);
    }

    private static async Task TagAndPushImage(string localTag, string targetTag, CancellationToken cancellationToken, IResourceContainerImageBuilder containerImageBuilder)
    {
        await containerImageBuilder.TagImageAsync(localTag, targetTag, cancellationToken).ConfigureAwait(false);
        await containerImageBuilder.PushImageAsync(targetTag, cancellationToken).ConfigureAwait(false);
    }

    public static string TryGetComputeResourceEndpoint(IResource computeResource, IAzureComputeEnvironmentResource azureComputeEnv)
    {
        // Only produce endpoints for resources that have external endpoints
        if (!computeResource.TryGetEndpoints(out var endpoints) || !endpoints.Any(e => e.IsExternal))
        {
            return string.Empty;
        }

        // For Azure Container Apps, use the ContainerAppDomain
        if (azureComputeEnv is AzureProvisioningResource provisioningResource &&
            provisioningResource.Outputs.TryGetValue("AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN", out var domainValue))
        {
            var endpoint = $"https://{computeResource.Name.ToLowerInvariant()}.{domainValue}";
            return $" to [{endpoint}]({endpoint})";
        }

        // For Azure App Service, construct the URL using the WebSiteSuffix
        if (azureComputeEnv is AzureProvisioningResource appServiceResource &&
            appServiceResource.Outputs.TryGetValue("webSiteSuffix", out var suffixValue))
        {
            var endpoint = $"https://{computeResource.Name.ToLowerInvariant()}-{suffixValue}.azurewebsites.net";
            return $" to [{endpoint}]({endpoint})";
        }

        return string.Empty;
    }
}
