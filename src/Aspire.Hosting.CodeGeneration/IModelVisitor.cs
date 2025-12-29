// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.CodeGeneration.Models;
using Aspire.Hosting.CodeGeneration.Models.Types;

namespace Aspire.Hosting.CodeGeneration;

/// <summary>
/// Visitor interface for traversing the application model.
/// Language-specific code generators implement this to generate code.
/// </summary>
public interface IModelVisitor
{
    /// <summary>
    /// Visit the root application model.
    /// </summary>
    void VisitApplicationModel(ApplicationModel model);

    /// <summary>
    /// Visit the distributed application builder model.
    /// </summary>
    void VisitBuilderModel(DistributedApplicationBuilderModel model);

    /// <summary>
    /// Visit a proxy type that wraps a .NET type for the target language.
    /// </summary>
    void VisitProxyType(RoType type, ProxyTypeModel proxyModel);

    /// <summary>
    /// Visit an integration (Aspire package).
    /// </summary>
    void VisitIntegration(IntegrationModel integration);

    /// <summary>
    /// Visit a resource type.
    /// </summary>
    void VisitResource(ResourceModel resource);

    /// <summary>
    /// Visit a method to generate.
    /// </summary>
    void VisitMethod(RoMethod method, MethodContext context);

    /// <summary>
    /// Visit a property to generate.
    /// </summary>
    void VisitProperty(RoPropertyInfo property, PropertyContext context);
}

/// <summary>
/// Context for method generation, indicating what kind of method is being generated.
/// </summary>
public enum MethodContext
{
    /// <summary>Instance method on a proxy class.</summary>
    ProxyInstance,

    /// <summary>Static method on a proxy class.</summary>
    ProxyStatic,

    /// <summary>Extension method on IDistributedApplicationBuilder.</summary>
    BuilderExtension,

    /// <summary>Extension method on IResourceBuilder.</summary>
    ResourceExtension
}

/// <summary>
/// Context for property generation, indicating what kind of property is being generated.
/// </summary>
public enum PropertyContext
{
    /// <summary>Property on a proxy class.</summary>
    ProxyProperty,

    /// <summary>Property on the builder.</summary>
    BuilderProperty,

    /// <summary>Property on the application.</summary>
    ApplicationProperty
}

/// <summary>
/// Represents a method with its disambiguated name for code generation.
/// The base class handles grouping overloads and assigning unique names.
/// </summary>
/// <param name="Method">The method metadata.</param>
/// <param name="UniqueName">The disambiguated method name (e.g., "addRedis", "addRedis2").</param>
/// <param name="OverloadIndex">Zero-based index within the overload group.</param>
public record MethodOverload(
    RoMethod Method,
    string UniqueName,
    int OverloadIndex);

/// <summary>
/// Context for a method parameter during code generation.
/// </summary>
/// <param name="Name">The formatted parameter name for the target language.</param>
/// <param name="Type">The formatted type name for the target language.</param>
/// <param name="OriginalType">The original .NET parameter type.</param>
/// <param name="IsCallback">Whether this parameter is a delegate/callback type.</param>
/// <param name="Original">The original parameter metadata.</param>
public record MethodParameterContext(
    string Name,
    string Type,
    RoType OriginalType,
    bool IsCallback,
    RoParameterInfo Original);

/// <summary>
/// Context for proxy method generation containing all information needed to emit the method.
/// </summary>
/// <param name="MethodName">The disambiguated, formatted method name.</param>
/// <param name="OriginalMethodName">The original .NET method name (for RPC calls).</param>
/// <param name="Parameters">The formatted parameters.</param>
/// <param name="ReturnType">The formatted return type.</param>
/// <param name="OriginalReturnType">The original .NET return type.</param>
/// <param name="IsVoid">Whether the method returns void.</param>
/// <param name="IsStatic">Whether this is a static method.</param>
/// <param name="DeclaringType">The type that declares this method (for static invocation).</param>
/// <param name="Overload">The original method overload information.</param>
public record ProxyMethodContext(
    string MethodName,
    string OriginalMethodName,
    IReadOnlyList<MethodParameterContext> Parameters,
    string ReturnType,
    RoType OriginalReturnType,
    bool IsVoid,
    bool IsStatic,
    RoType? DeclaringType,
    MethodOverload Overload);
