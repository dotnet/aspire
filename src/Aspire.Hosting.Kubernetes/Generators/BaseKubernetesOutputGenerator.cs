// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using k8s;
using k8s.Models;
using Microsoft.Extensions.Logging;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Aspire.Hosting.Kubernetes.Generators;

internal abstract class BaseKubernetesOutputGenerator(
    DistributedApplicationModel model,
    DistributedApplicationExecutionContext executionContext,
    string outputDirectory,
    ILogger logger,
    CancellationToken cancellationToken)
{
    protected DistributedApplicationModel Model { get; } = model;

    protected DistributedApplicationExecutionContext ExecutionContext { get; } = executionContext;

    protected string OutputDirectory { get; } = outputDirectory;

    protected ILogger Logger { get; } = logger;

    protected CancellationToken CancellationToken { get; } = cancellationToken;

    protected readonly ISerializer YamlSerializer = new SerializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
        .Build();

    public abstract Task WriteManifests();

    protected IEnumerable<IKubernetesObject<V1ObjectMeta>> CreateManifestsFromModel()
    {
        // TODO: Create manifests for each IResource ...
        _ = ExecutionContext;

        return [];
    }
}
