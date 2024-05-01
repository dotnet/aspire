// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.AWS.Lambda.Utils;
using Aspire.Hosting.Lifecycle;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.AWS.Lambda;

internal sealed class AWSLambdaToolsPublisher(
    DistributedApplicationExecutionContext executionContext,
    ILogger<AWSLambdaToolsPublisher> logger,
    IConfiguration configuration) : IDistributedApplicationLifecycleHook
{
    private const string FunctionHandlerPropertyName = "function-handler";
    private const string FunctionRuntimePropertyName = "function-runtime";

    public async Task BeforeStartAsync(DistributedApplicationModel appModel,
        CancellationToken cancellationToken = default)
    {
        if (!executionContext.IsPublishMode)
        {
            return;
        }

        var resources = appModel.Resources.OfType<ILambdaFunction>().ToList();

        if (resources.Count == 0)
        {
            logger.LogWarning(
                "AWSLambdaToolsSupport has been added but no Lambda Functions have been expressed in the Application Model.");
            return;
        }

        var projectFunctions = resources.GroupBy(x => x.GetFunctionMetadata().ProjectPath);

        var manifests = new Dictionary<string, Func<Task>>();
        var createMissing = string.Equals(configuration.GetSection(Constants.AwsLambdaToolsCreateMissing).Value, "true",
            StringComparison.OrdinalIgnoreCase);

        foreach (var project in projectFunctions)
        {
            foreach (var function in project)
            {
                var metadata = function.GetFunctionMetadata();
                var name = function.Name;

                if (project.Count() == 1 || function.TryGetLastAnnotation<DefaultFunction>(out _))
                {
                    name = "defaults";
                }

                var fileName = Path.Combine(Path.GetDirectoryName(metadata.ProjectPath)!,
                    $"aws-lambda-tools-{name}.json");

                if (manifests.ContainsKey(fileName))
                {
                    throw new AWSLambdaException(
                        $"More than a single Lambda Function have been configured as '{name}' for project: {metadata.ProjectPath}");
                }

                manifests.Add(fileName,
                    UpdateToolsFile(fileName, metadata.Handler, function.Runtime.Name, createMissing));
            }
        }

        var tasks = manifests.Select(x => x.Value.Invoke()).ToList();
        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    private Func<Task> UpdateToolsFile(string fileName, string handler, string runtime, bool createMissing)
    {
        return async () =>
        {
            Dictionary<string, object?> toolsFile;

            if (File.Exists(fileName))
            {
                var contents = await File.ReadAllTextAsync(fileName).ConfigureAwait(false);
                toolsFile = JsonSerializer.Deserialize<Dictionary<string, object?>>(contents) ?? [];
            }
            else if (!createMissing)
            {
                logger.LogInformation("File '{fileName}' does not exist. Publish with '--{option} true' to create.",
                    fileName, Constants.AwsLambdaToolsCreateMissing);
                return;
            }
            else
            {
                toolsFile = LambdaFilesUtils.CreateEmptyToolsFile();
            }

            var changesApplied = false;

            if (toolsFile.TryGetValue(FunctionHandlerPropertyName, out var existingHandler))
            {
                if (!string.Equals(handler, existingHandler?.ToString(), StringComparison.Ordinal))
                {
                    changesApplied = true;
                    toolsFile[FunctionHandlerPropertyName] = handler;
                }
            }
            else
            {
                toolsFile.Add(FunctionHandlerPropertyName, handler);
                changesApplied = true;
            }

            if (toolsFile.TryGetValue(FunctionRuntimePropertyName, out var existingRuntime))
            {
                if (!string.Equals(runtime, existingRuntime?.ToString(), StringComparison.Ordinal))
                {
                    changesApplied = true;
                    toolsFile[FunctionRuntimePropertyName] = runtime;
                }
            }
            else
            {
                toolsFile.Add(FunctionRuntimePropertyName, runtime);
                changesApplied = true;
            }

            if (!changesApplied)
            {
                return;
            }

            logger.LogInformation("Writing {fileName}", fileName);
            var content = JsonSerializer.Serialize(toolsFile, LambdaFilesUtils.SerializerOptions);
            await File.WriteAllTextAsync(fileName, content).ConfigureAwait(false);
        };
    }
}
