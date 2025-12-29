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
/// Information about a method parameter for code generation.
/// </summary>
/// <param name="Name">The parameter name.</param>
/// <param name="Type">The formatted type string for the target language.</param>
/// <param name="IsCallback">Whether this parameter is a delegate/callback type.</param>
/// <param name="IsOptional">Whether this parameter is optional.</param>
/// <param name="DefaultValue">The default value expression, if any.</param>
public record ParameterInfo(
    string Name,
    string Type,
    bool IsCallback,
    bool IsOptional = false,
    string? DefaultValue = null);
