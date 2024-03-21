// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Dcp;
using Aspire.Hosting.Dcp.Model;
using k8s;

namespace Aspire.Hosting.Tests.Dcp;

internal sealed class MockKubernetesService : IKubernetesService
{
    internal sealed record DeletedResource(Type Type, object Value);

    public List<object> CreatedResources { get; } = [];

    public Task<T> CreateAsync<T>(T obj, CancellationToken cancellationToken = default) where T : CustomResource
    {
        CreatedResources.Add(obj);
        return Task.FromResult(obj);
    }

    public Task<T> DeleteAsync<T>(string name, string? namespaceParameter = null, CancellationToken cancellationToken = default) where T : CustomResource
    {
        throw new NotImplementedException();
    }

    public Task<List<T>> ListAsync<T>(string? namespaceParameter = null, CancellationToken cancellationToken = default) where T : CustomResource
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<(WatchEventType, T)> WatchAsync<T>(string? namespaceParameter = null, CancellationToken cancellationToken = default) where T : CustomResource
    {
        throw new NotImplementedException();
    }

    public Task<Stream> GetLogStreamAsync<T>(T obj, string logStreamType, bool? follow = true, bool? timestamps = false, CancellationToken cancellationToken = default) where T : CustomResource
    {
        throw new NotImplementedException();
    }
}
