// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Docker.Resources;
using Aspire.Hosting.Publishing;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.Docker;

/// <summary>
/// Contextual information used for manifest publishing during this execution of the AppHost as docker compose output format.
/// </summary>
/// <param name="executionContext">Global contextual information for this invocation of the AppHost.</param>
/// <param name="outputPath">Output path for assets generated via this invocation of the AppHost.</param>
/// <param name="logger">The current publisher logger instance.</param>
/// <param name="cancellationToken">Cancellation token for this operation.</param>
internal sealed class DockerComposePublishingContext(DistributedApplicationExecutionContext executionContext, string outputPath, ILogger logger, CancellationToken cancellationToken = default)
{
    internal async Task WriteModel(DistributedApplicationModel model)
    {
        logger.StartGeneratingDockerCompose();

        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(outputPath);

        if (model.Resources.Count == 0)
        {
            logger.WriteMessage("No resources found in the model.");
            return;
        }

        var outputFile = await WriteDockerComposeOutput(model).ConfigureAwait(false);

        logger.FinishGeneratingDockerCompose(outputFile);
    }

    private async Task<string> WriteDockerComposeOutput(DistributedApplicationModel model)
    {
        var composeFile = new ComposeFile();

        foreach (var resource in model.Resources)
        {
            await HandleResourceAsync(resource, composeFile).ConfigureAwait(false);
        }

        var composeOutput = composeFile.ToYamlString();
        var outputFile = Path.Combine(outputPath, "docker-compose.yaml");
        Directory.CreateDirectory(outputPath);
        await File.WriteAllTextAsync(outputFile, composeOutput, cancellationToken).ConfigureAwait(false);
        return outputFile;
    }

    private async Task HandleResourceAsync(IResource resource, ComposeFile composeFile)
    {
        var composeService = resource switch
        {
            ContainerResource containerResource => await HandleContainerResource(containerResource).ConfigureAwait(false),
            ProjectResource projectResource => await HandleProjectResource(projectResource).ConfigureAwait(false),
            _ => null,
        };

        if (composeService is null)
        {
            return;
        }

        composeFile.AddService(composeService.Name!, composeService);
    }

    private async Task<ComposeService?> HandleContainerResource(ContainerResource containerResource)
    {
        if (!containerResource.TryGetContainerImageName(out var containerImage))
        {
            logger.FailedToGetContainerImage(containerResource.Name);
            return null;
        }

        var service = await AddResourceAsService(containerResource).ConfigureAwait(false);
        return service?.WithImage(containerImage);
    }

    private async Task<ComposeService?> HandleProjectResource(ProjectResource projectResource)
    {
        var service = await AddResourceAsService(projectResource).ConfigureAwait(false);
        var projectMetadata = projectResource.GetProjectImageMetadata(logger); // TODO: Simply returning path to project for now, the extension will call dotnet to grab project properties for Container image details.

        if (string.IsNullOrEmpty(projectMetadata))
        {
            logger.FailedToGetContainerImage(projectResource.Name);
            return null;
        }

        return service?.WithImage(projectMetadata);
    }

    private async Task<ComposeService?> AddResourceAsService(IResource resource)
    {
        var service = new ComposeService(resource.Name.ToLowerInvariant());

        await PopulateServiceEnvironmentVariables(resource, service).ConfigureAwait(false);
        await PopulateServiceArguments(resource, service).ConfigureAwait(false);

        return service;
    }

    private async Task PopulateServiceEnvironmentVariables(IResource resource, ComposeService service)
    {
        var env = await executionContext.GetEnvironmentalVariablesForResource(resource).ConfigureAwait(false);

        if (env.Count == 0)
        {
            return;
        }

        foreach (var (key, value) in env)
        {
            service.AddEnvironmentVariable(key, value.Item2); // TODO: Support processing the unprocessed values
        }
    }

    private async Task PopulateServiceArguments(IResource resource, ComposeService service)
    {
        var args = await executionContext.GetCommandLineArgumentsForResource(resource).ConfigureAwait(false);

        if (args.Count == 0)
        {
            return;
        }

        foreach (var value in args)
        {
            service.AddCommand(value.Item2); // TODO: Support processing the unprocessed values
        }
    }
}
