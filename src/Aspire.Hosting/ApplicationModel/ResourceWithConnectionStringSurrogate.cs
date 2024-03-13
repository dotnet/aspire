// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

internal sealed class ResourceWithConnectionStringSurrogate(IResource innerResource, Func<string> callback, string? environmentVariableName) : IResourceWithConnectionString
{
    public string Name => innerResource.Name;

    public ResourceAnnotationCollection Annotations => innerResource.Annotations;

    public ValueTask<string?> GetConnectionStringAsync(CancellationToken cancellationToken)
    {
        return new(callback());
    }

    public string? ConnectionStringEnvironmentVariable => environmentVariableName;
}
