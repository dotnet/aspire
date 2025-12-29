// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.CodeGeneration.Models.Types;

namespace Aspire.Hosting.CodeGeneration.Models;

/// <summary>
/// Model representing the DistributedApplicationBuilder and DistributedApplication types
/// for code generation purposes.
/// </summary>
public sealed class DistributedApplicationBuilderModel
{
    /// <summary>
    /// Properties from IDistributedApplicationBuilder interface.
    /// </summary>
    public required IReadOnlyList<RoPropertyInfo> BuilderProperties { get; init; }

    /// <summary>
    /// Methods from IDistributedApplicationBuilder interface (e.g., Build).
    /// </summary>
    public required IReadOnlyList<RoMethod> BuilderMethods { get; init; }

    /// <summary>
    /// Static methods from DistributedApplication class (CreateBuilder overloads).
    /// </summary>
    public required IReadOnlyList<RoMethod> StaticFactoryMethods { get; init; }

    /// <summary>
    /// Instance methods from DistributedApplication class (Run, RunAsync, etc.).
    /// </summary>
    public required IReadOnlyList<RoMethod> ApplicationMethods { get; init; }

    /// <summary>
    /// Properties from DistributedApplication class.
    /// </summary>
    public required IReadOnlyList<RoPropertyInfo> ApplicationProperties { get; init; }

    /// <summary>
    /// Types that need proxy wrapper classes (e.g., IConfiguration, IHostEnvironment).
    /// </summary>
    public required Dictionary<RoType, ProxyTypeModel> ProxyTypes { get; init; }
}

/// <summary>
/// Model representing a type that needs a TypeScript proxy wrapper class.
/// </summary>
public sealed class ProxyTypeModel
{
    /// <summary>
    /// The .NET type being proxied.
    /// </summary>
    public required RoType Type { get; init; }

    /// <summary>
    /// The TypeScript proxy class name (e.g., "ConfigurationProxy").
    /// </summary>
    public required string ProxyClassName { get; init; }

    /// <summary>
    /// Properties to expose on the proxy.
    /// </summary>
    public required IReadOnlyList<RoPropertyInfo> Properties { get; init; }

    /// <summary>
    /// Instance methods to expose on the proxy.
    /// </summary>
    public required IReadOnlyList<RoMethod> Methods { get; init; }

    /// <summary>
    /// Static methods to expose on the proxy.
    /// </summary>
    public required IReadOnlyList<RoMethod> StaticMethods { get; init; }

    /// <summary>
    /// Custom helper methods that aren't directly reflected.
    /// </summary>
    public List<HelperMethodDefinition> HelperMethods { get; init; } = [];
}

/// <summary>
/// Definition for a helper method that cannot be purely reflected
/// (e.g., isDevelopment() which compares environment name).
/// </summary>
public sealed class HelperMethodDefinition
{
    /// <summary>
    /// The TypeScript method name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// The TypeScript return type.
    /// </summary>
    public required string ReturnType { get; init; }

    /// <summary>
    /// The TypeScript method body (implementation).
    /// </summary>
    public required string Body { get; init; }

    /// <summary>
    /// Optional JSDoc documentation.
    /// </summary>
    public string? Documentation { get; init; }

    /// <summary>
    /// Optional parameters for the method.
    /// </summary>
    public List<HelperMethodParameter> Parameters { get; init; } = [];
}

/// <summary>
/// A parameter for a helper method.
/// </summary>
public sealed class HelperMethodParameter
{
    /// <summary>
    /// The parameter name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// The TypeScript type.
    /// </summary>
    public required string Type { get; init; }
}
