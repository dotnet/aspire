// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

internal sealed class ResourceWithConnectionStringSurrogate : ParameterResource, IResourceWithConnectionString
{
    private readonly string? _environmentVariableName;

    public ResourceWithConnectionStringSurrogate(string name, Func<ParameterDefault?, string> callback, string? environmentVariableName) : base(name, callback, secret: true)
    {
        _environmentVariableName = environmentVariableName;

        IsConnectionString = true;
    }

    string IManifestExpressionProvider.ValueExpression => $"{{{Name}.connectionString}}";

    public string? ConnectionStringEnvironmentVariable => _environmentVariableName;

    public ReferenceExpression ConnectionStringExpression =>
        ReferenceExpression.Create($"{this}");
}
