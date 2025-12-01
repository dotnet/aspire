// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.ApplicationModel;

internal class ResourceExecutionConfigurationGathererContext : IResourceExecutionConfigurationGathererContext
{
    /// <inheritdoc/>
    public required IResource Resource { get; init; }

    /// <inheritdoc/>
    public required ILogger ResourceLogger { get; init; }

    /// <inheritdoc/>
    public required DistributedApplicationExecutionContext ExecutionContext { get; init; }

    /// <inheritdoc/>
    public List<object> Arguments { get; } = new();

    /// <inheritdoc/>
    public Dictionary<string, object> EnvironmentVariables { get; } = new();

    /// <summary>
    /// Additional configuration data collected during gathering.
    /// </summary>
    internal HashSet<IResourceExecutionConfigurationData> AdditionalConfigurationData { get; } = new();

    /// <inheritdoc/>
    public void AddAdditionalData(IResourceExecutionConfigurationData metadata)
    {
        AdditionalConfigurationData.Add(metadata);
    }

    /// <summary>
    /// Resolves the actual <see cref="IResourceExecutionConfiguration"/> from the gatherer context.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains the resolved resource configuration.
    /// </returns>
    internal async Task<IResourceExecutionConfiguration> ResolveAsync(CancellationToken cancellationToken = default)
    {
        List<(string value, bool isSensitive)> resolvedArguments = new(Arguments.Count);
        Dictionary<string, string> resolvedEnvironmentVariables = new(EnvironmentVariables.Count);
        List<Exception> exceptions = new();

        foreach (var argument in Arguments)
        {
            try
            {
                var resolvedValue = await Resource.ResolveValueAsync(ExecutionContext, ResourceLogger, argument, null, cancellationToken).ConfigureAwait(false);
                if (resolvedValue?.Value != null)
                {
                    resolvedArguments.Add((resolvedValue.Value, resolvedValue.IsSensitive));
                }
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        }

        foreach (var kvp in EnvironmentVariables)
        {
            try
            {
                var resolvedValue = await Resource.ResolveValueAsync(ExecutionContext, ResourceLogger, kvp.Value, null, cancellationToken).ConfigureAwait(false);
                if (resolvedValue?.Value != null)
                {
                    resolvedEnvironmentVariables[kvp.Key] = resolvedValue.Value;
                }
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        }

        return new ResourceExecutionConfiguration
        {
            Arguments = resolvedArguments,
            EnvironmentVariables = resolvedEnvironmentVariables,
            AdditionalConfigurationData = AdditionalConfigurationData,
            Exception = exceptions.Count == 0 ? null : new AggregateException("One or more errors occurred while resolving resource configuration.", exceptions)
        };
    }
}