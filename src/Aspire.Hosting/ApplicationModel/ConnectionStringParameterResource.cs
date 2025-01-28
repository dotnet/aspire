// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents a connection string resource.
/// </summary>
public sealed class ConnectionStringParameterResource : ParameterResource, IResourceWithConnectionString
{
    private readonly string? _environmentVariableName;

    /// <summary>
    /// Initializes a new instance of <see cref="ConnectionStringParameterResource"/>.
    /// </summary>
    /// <param name="name">The name of the parameter resource.</param>
    /// <param name="callback">The callback function to retrieve the value of the parameter.</param>
    /// <param name="environmentVariableName">The name of the environment variable that contains the connection string.</param>
    public ConnectionStringParameterResource(string name, Func<ParameterDefault?, string> callback, string? environmentVariableName) : base(name, callback, secret: true)
    {
        _environmentVariableName = environmentVariableName;
        IsConnectionString = true;
        Annotations.Add(new ConnectionStringAnnotation(GetConnectionString));
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="name"></param>
    /// <param name="resourceAnnotations"></param>
    public ConnectionStringParameterResource(string name, ResourceAnnotationCollection resourceAnnotations) : base(name, resourceAnnotations)
    {
        IsConnectionString = true;
        Secret = true;
        Annotations.Add(new ConnectionStringAnnotation(GetConnectionString));
    }

    /// <summary>
    /// Gets the name of the environment variable that contains the connection string.
    /// </summary>
    public string? ConnectionStringEnvironmentVariable => _environmentVariableName;

    private ReferenceExpression GetConnectionString() =>
        ReferenceExpression.Create($"{this}");
}