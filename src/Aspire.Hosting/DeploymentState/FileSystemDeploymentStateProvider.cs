// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREPUBLISHERS001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Aspire.Hosting.Publishing;

namespace Aspire.Hosting.DeploymentState;

internal sealed class FileSystemDeploymentStateProvider(ILogger<FileSystemDeploymentStateProvider> logger, IOptions<PublishingOptions> publishingOptions) : IDeploymentStateProvider
{
    private const string DefaultStateDirectory = ".aspire";
    private const string DefaultStateFileName = "deployment-state.json";

    private static readonly JsonSerializerOptions s_jsonSerializerOptions = new()
    {
        WriteIndented = true
    };

    private string GetStateFilePath()
    {
        // Use output path from publishing options if available, otherwise use default
        var baseDirectory = publishingOptions.Value.OutputPath ?? Directory.GetCurrentDirectory();
        var stateDirectory = Path.Combine(baseDirectory, DefaultStateDirectory);
        return Path.Combine(stateDirectory, DefaultStateFileName);
    }

    public async Task<JsonObject> LoadAsync(CancellationToken cancellationToken = default)
    {
        var stateFilePath = GetStateFilePath();

        var jsonDocumentOptions = new JsonDocumentOptions
        {
            CommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
        };

        if (File.Exists(stateFilePath))
        {
            try
            {
                var content = await File.ReadAllTextAsync(stateFilePath, cancellationToken).ConfigureAwait(false);
                var state = JsonNode.Parse(content, documentOptions: jsonDocumentOptions)?.AsObject();
                return state ?? [];
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to load deployment state from {Path}. Starting with empty state.", stateFilePath);
                return [];
            }
        }

        return [];
    }

    public async Task SaveAsync(JsonObject state, CancellationToken cancellationToken = default)
    {
        try
        {
            var stateFilePath = GetStateFilePath();
            var stateDirectory = Path.GetDirectoryName(stateFilePath)!;

            Directory.CreateDirectory(stateDirectory);

            await File.WriteAllTextAsync(stateFilePath, state.ToJsonString(s_jsonSerializerOptions), cancellationToken).ConfigureAwait(false);

            logger.LogInformation("Deployment state saved to {Path}.", stateFilePath);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to save deployment state.");
        }
    }
}
