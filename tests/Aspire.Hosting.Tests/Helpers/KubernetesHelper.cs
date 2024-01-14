// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Dcp;
using Aspire.Hosting.Dcp.Model;
using k8s.Models;

namespace Aspire.Hosting.Tests.Helpers;

internal static class KubernetesHelper
{
    public static async Task<T> GetResourceByNameAsync<T>(KubernetesService kubernetes, string resourceName, Func<T, bool> ready, CancellationToken cancellationToken) where T : CustomResource
    {
        await foreach (var (_, r) in kubernetes!.WatchAsync<T>(cancellationToken: cancellationToken))
        {
            var name = r.Name();

            if ((name == resourceName || name.StartsWith(resourceName + "-", StringComparison.Ordinal)) && ready(r))
            {
                return r;
            }
        }

        throw new InvalidOperationException($"Resource {resourceName}, not ready");
    }
}
