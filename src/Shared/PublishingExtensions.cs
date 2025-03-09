// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.ExceptionServices;
using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Aspire.Hosting.Publishing;

internal static class PublishingExtensions
{
    internal static async Task<Dictionary<string, (object, string)>> GetEnvironmentalVariablesForResource(this DistributedApplicationExecutionContext executionContext, IResource resource, CancellationToken cancellationToken = default)
    {
        var env = new Dictionary<string, (object, string)>();

        await resource.ProcessEnvironmentVariableValuesAsync(
            executionContext,
            (key, unprocessed, processed, ex) =>
            {
                if (ex is not null)
                {
                    ExceptionDispatchInfo.Throw(ex);
                }

                if (unprocessed is not null && processed is not null)
                {
                    env[key] = (unprocessed, processed);
                }
            },
            NullLogger.Instance,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return env;
    }

    internal static async Task<List<(object, string)>> GetCommandLineArgumentsForResource(this DistributedApplicationExecutionContext executionContext, IResource resource, CancellationToken cancellationToken = default)
    {
        var args = new List<(object, string)>();

        await resource.ProcessArgumentValuesAsync(
            executionContext,
            (unprocessed, expression, ex, _) =>
            {
                if (ex is not null)
                {
                    ExceptionDispatchInfo.Throw(ex);
                }

                if (unprocessed is not null && expression is not null)
                {
                    args.Add((unprocessed, expression));
                }
            },
            NullLogger.Instance,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return args;
    }

    internal static string? GetProjectImageMetadata(this ProjectResource resource, ILogger? logger = null)
    {
        if (!resource.TryGetLastAnnotation<IProjectMetadata>(out var metadata))
        {
            logger?.LogWarning("No project metadata found for Project: '{ResourceName}'", resource.Name);
            return null;
        }

        if (string.IsNullOrEmpty(metadata.ProjectPath))
        {
            logger?.LogWarning("Project path is not set for Project: '{ResourceName}'", resource.Name);
            return null;
        }

        if (!File.Exists(metadata.ProjectPath))
        {
            logger?.LogWarning("Project file does not exist: '{ProjectPath}'", metadata.ProjectPath);
            return null;
        }

        // TODO: We need to extract project based metadata from the project file with a cli call to dotnet to get project properties.
        return metadata.ProjectPath;
    }
}
