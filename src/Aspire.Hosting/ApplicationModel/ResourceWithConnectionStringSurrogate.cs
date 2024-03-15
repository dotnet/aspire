// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

internal sealed class ResourceWithConnectionStringSurrogate(IResource innerResource, Func<string> callback, string? environmentVariableName) : IResourceWithConnectionString
{
    public string Name => innerResource.Name;

    public ResourceAnnotationCollection Annotations => innerResource.Annotations;

    public string? ConnectionStringEnvironmentVariable => environmentVariableName;

    public ReferenceExpression ConnectionStringExpression =>
        ReferenceExpression.Create($"{callback()}");
}
