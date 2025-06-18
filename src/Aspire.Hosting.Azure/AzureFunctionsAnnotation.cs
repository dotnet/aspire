// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Azure;

/// <summary>
/// Used to annotate resources as Azure Functions.
/// </summary>
public sealed class AzureFunctionsAnnotation : IResourceAnnotation
{
    /// <summary>
    /// Gets the collection of keys associated with the Azure Functions resource.
    /// </summary>
    public List<AzureFunctionsKey> Keys { get; } = [];
}

/// <summary>
/// Represents the type of key used in Azure Functions.
/// </summary>
public enum AzureFunctionsKeyType
{
    /// <summary>
    /// Represents an administrative key.
    /// </summary>
    Admin,

    /// <summary>
    /// Represents a host-level key.
    /// </summary>
    Host,

    /// <summary>
    /// Represents a function-specific key.
    /// </summary>
    Function,

    /// <summary>
    /// Represents a system-level key.
    /// </summary>
    System
}

/// <summary>
/// </summary>
/// <param name="keyType"></param>
/// <param name="functionName"></param>
/// <param name="keyName"></param>
/// <param name="secretParameter"></param>
public class AzureFunctionsKey(AzureFunctionsKeyType keyType, string? functionName, string? keyName, ParameterResource secretParameter) : IResourceAnnotation
{
    /// <summary>
    /// Gets the type of the Azure Functions key.
    /// </summary>
    public AzureFunctionsKeyType KeyType { get; } = keyType;

    /// <summary>
    /// Gets the name of the function associated with the key, if applicable.
    /// </summary>
    public string? FunctionName { get; } = functionName;

    /// <summary>
    /// Gets the name of the key, if applicable.
    /// </summary>
    public string? KeyName { get; } = keyName;

    /// <summary>
    /// Gets the parameter resource that contains the secret value for the key.
    /// </summary>
    public ParameterResource SecretParameter { get; } = secretParameter;
}
