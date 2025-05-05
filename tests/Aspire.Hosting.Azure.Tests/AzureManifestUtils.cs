// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;
using Aspire.Hosting.Tests.Utils;
using Microsoft.Extensions.Options;

namespace Aspire.Hosting.Utils;

public sealed class AzureManifestUtils
{
    public static Task<(JsonNode ManifestNode, string BicepText)> GetManifestWithBicep(IResource resource, bool skipPreparer = false) =>
        GetManifestWithBicep(new DistributedApplicationModel([resource]), resource, skipPreparer);

    public static Task<(JsonNode ManifestNode, string BicepText)> GetManifestWithBicep(DistributedApplicationModel appModel, IResource resource) =>
        GetManifestWithBicep(appModel, resource, skipPreparer: false);

    private static async Task<(JsonNode ManifestNode, string BicepText)> GetManifestWithBicep(DistributedApplicationModel appModel, IResource resource, bool skipPreparer)
    {
        if (!skipPreparer)
        {
            var executionContext = new DistributedApplicationExecutionContext(DistributedApplicationOperation.Publish);
            var azurePreparer = new AzureResourcePreparer(Options.Create(new AzureProvisioningOptions()), executionContext, ResourceNotificationServiceTestHelpers.Create());
            await azurePreparer.BeforeStartAsync(appModel, cancellationToken: default);
        }

        string manifestDir = Directory.CreateTempSubdirectory(resource.Name).FullName;
        var manifestNode = await ManifestUtils.GetManifest(resource, manifestDir);

        if (!manifestNode.AsObject().TryGetPropertyValue("path", out var pathNode))
        {
            throw new ArgumentException("Specified resource does not contain a path property.", nameof(resource));
        }

        if (pathNode?.ToString() is not { } path || !File.Exists(Path.Combine(manifestDir, path)))
        {
            throw new ArgumentException("Path node in resource is null, empty, or does not exist.", nameof(resource));
        }

        var bicepText = await File.ReadAllTextAsync(Path.Combine(manifestDir, path));
        return (manifestNode, bicepText);
    }

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "ExecuteBeforeStartHooksAsync")]
    public static extern Task ExecuteBeforeStartHooksAsync(DistributedApplication app, CancellationToken cancellationToken);
}
