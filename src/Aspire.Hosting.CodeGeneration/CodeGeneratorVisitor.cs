// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Aspire.Hosting.CodeGeneration.Models;
using Aspire.Hosting.CodeGeneration.Models.Types;

namespace Aspire.Hosting.CodeGeneration;

/// <summary>
/// Abstract base class for code generators that visit the application model.
/// Contains shared logic for traversal, type detection, and method disambiguation
/// that can be reused across different language generators.
/// </summary>
public abstract class CodeGeneratorVisitor : IModelVisitor, ICodeGenerator
{
    /// <summary>
    /// The text writer for output.
    /// </summary>
    protected TextWriter Writer { get; set; } = null!;

    /// <summary>
    /// The application model being generated.
    /// </summary>
    protected ApplicationModel Model { get; set; } = null!;

    /// <summary>
    /// Tracks emitted type names to avoid duplicates.
    /// </summary>
    protected HashSet<string> EmittedTypes { get; } = [];

    /// <inheritdoc />
    public abstract string Language { get; }

    /// <summary>
    /// Gets the main output file name (e.g., "distributed-application.ts").
    /// </summary>
    protected virtual string MainFileName => "output.txt";

    // === Main entry point ===

    /// <inheritdoc />
    public virtual Dictionary<string, string> GenerateDistributedApplication(ApplicationModel model)
    {
        Model = model;
        var files = new Dictionary<string, string>();

        using var writer = new StringWriter();
        Writer = writer;

        // Use the shared traversal pattern
        VisitApplicationModel(model);

        files[MainFileName] = writer.ToString();
        AddEmbeddedResources(files);

        return files;
    }

    // === Shared traversal logic ===

    /// <inheritdoc />
    public virtual void VisitApplicationModel(ApplicationModel model)
    {
        // 1. Emit header (imports, client setup)
        EmitHeader();

        // 2. Emit createBuilder() function
        EmitCreateBuilderFunction();

        // 3. Emit DistributedApplication class
        EmitDistributedApplicationClass();

        // 4. Emit enums used by proxy classes
        EmitBuilderEnums();

        // 5. Visit and emit proxy classes
        VisitBuilderModel(model.BuilderModel);

        // 6. Emit builder base class
        EmitBuilderBaseClass();

        // 7. Emit DistributedApplicationBuilder with integration methods
        EmitBuilderClassStart();
        foreach (var integration in model.IntegrationModels.Values)
        {
            VisitIntegration(integration);
        }
        EmitBuilderClassEnd();

        // 8. Emit base resource builder classes
        EmitBaseResourceBuilderClasses();

        // 9. Visit and emit resource-specific builder classes
        foreach (var resource in model.ResourceModels.Values)
        {
            VisitResource(resource);
        }

        // 10. Emit model classes (enums from integrations)
        EmitModelClasses();

        // 11. Emit any additional classes (parameter classes, callback proxies, etc.)
        EmitAdditionalClasses();
    }

    /// <inheritdoc />
    public virtual void VisitBuilderModel(DistributedApplicationBuilderModel builderModel)
    {
        foreach (var (type, proxyModel) in builderModel.ProxyTypes)
        {
            VisitProxyType(type, proxyModel);
        }
    }

    /// <inheritdoc />
    public virtual void VisitProxyType(RoType type, ProxyTypeModel proxyModel)
    {
        // Emit the proxy class start
        EmitProxyClassStart(type, proxyModel);

        // Visit properties
        foreach (var property in proxyModel.Properties.Where(p => !p.IsStatic))
        {
            VisitProperty(property, PropertyContext.ProxyProperty);
        }

        // Visit instance methods (base class handles grouping and disambiguation)
        foreach (var overload in GetMethodOverloads(proxyModel.Methods))
        {
            EmitProxyMethod(overload, isStatic: false);
        }

        // Emit helper methods (language-specific)
        EmitProxyHelperMethods(proxyModel);

        // Visit static methods
        foreach (var overload in GetMethodOverloads(proxyModel.StaticMethods))
        {
            EmitProxyMethod(overload, isStatic: true);
        }

        // Emit the proxy class end
        EmitProxyClassEnd(type, proxyModel);
    }

    /// <inheritdoc />
    public virtual void VisitIntegration(IntegrationModel integration)
    {
        // Emit methods from this integration onto the builder (base class handles grouping)
        foreach (var overload in GetMethodOverloads(integration.IDistributedApplicationBuilderExtensionMethods))
        {
            EmitExtensionMethod(overload);
        }
    }

    /// <inheritdoc />
    public virtual void VisitResource(ResourceModel resource)
    {
        // Emit the resource builder class start
        EmitResourceBuilderClassStart(resource);

        // Visit extension methods on this resource type (base class handles grouping)
        foreach (var overload in GetMethodOverloads(resource.IResourceTypeBuilderExtensionsMethods))
        {
            EmitExtensionMethod(overload);
        }

        // Emit the resource builder class end
        EmitResourceBuilderClassEnd(resource);
    }

    /// <inheritdoc />
    public virtual void VisitMethod(RoMethod method, MethodContext context)
    {
        // This method is kept for interface compatibility but traversal now uses
        // GetMethodOverloads + EmitProxyMethod/EmitExtensionMethod directly
    }

    /// <inheritdoc />
    public virtual void VisitProperty(RoPropertyInfo property, PropertyContext context)
    {
        EmitProperty(property, context);
    }

    // === Virtual methods for language-specific emission (override in derived classes) ===

    /// <summary>Emit file header (imports, client initialization).</summary>
    protected virtual void EmitHeader() { }

    /// <summary>Emit the createBuilder() factory function.</summary>
    protected virtual void EmitCreateBuilderFunction() { }

    /// <summary>Emit the DistributedApplication class.</summary>
    protected virtual void EmitDistributedApplicationClass() { }

    /// <summary>Emit enum definitions used by proxy classes.</summary>
    protected virtual void EmitBuilderEnums() { }

    /// <summary>Emit the builder base class (DistributedApplicationBuilderBase).</summary>
    protected virtual void EmitBuilderBaseClass() { }

    /// <summary>Emit the start of the DistributedApplicationBuilder class.</summary>
    protected virtual void EmitBuilderClassStart() { }

    /// <summary>Emit the end of the DistributedApplicationBuilder class.</summary>
    protected virtual void EmitBuilderClassEnd() { }

    /// <summary>Emit base resource builder classes (ResourceBuilder, etc.).</summary>
    protected virtual void EmitBaseResourceBuilderClasses() { }

    /// <summary>Emit model classes (enums from integrations).</summary>
    protected virtual void EmitModelClasses() { }

    /// <summary>Emit additional classes (parameter classes, callback proxies, etc.).</summary>
    protected virtual void EmitAdditionalClasses() { }

    /// <summary>Add embedded resource files to the output.</summary>
    protected virtual void AddEmbeddedResources(Dictionary<string, string> files) { }

    /// <summary>Emit the start of a proxy class.</summary>
    protected virtual void EmitProxyClassStart(RoType type, ProxyTypeModel proxyModel) { }

    /// <summary>Emit helper methods for a proxy class (between properties/methods and static methods).</summary>
    protected virtual void EmitProxyHelperMethods(ProxyTypeModel proxyModel) { }

    /// <summary>Emit the end of a proxy class.</summary>
    protected virtual void EmitProxyClassEnd(RoType type, ProxyTypeModel proxyModel) { }

    /// <summary>Emit the start of a resource builder class.</summary>
    protected virtual void EmitResourceBuilderClassStart(ResourceModel resource) { }

    /// <summary>Emit the end of a resource builder class.</summary>
    protected virtual void EmitResourceBuilderClassEnd(ResourceModel resource) { }

    /// <summary>Emit a proxy method (instance or static). UniqueName is already disambiguated.</summary>
    protected virtual void EmitProxyMethod(MethodOverload overload, bool isStatic) { }

    /// <summary>Emit an extension method (on builder or resource). UniqueName is already disambiguated.</summary>
    protected virtual void EmitExtensionMethod(MethodOverload overload) { }

    /// <summary>Emit a property.</summary>
    protected virtual void EmitProperty(RoPropertyInfo property, PropertyContext context) { }

    /// <summary>Format a type for the target language.</summary>
    protected virtual string FormatType(RoType type) => type.Name;

    /// <summary>Format a method name for the target language (e.g., camelCase, snake_case).</summary>
    protected virtual string FormatMethodName(string name) => name;

    // === Shared utility methods ===

    /// <summary>
    /// Groups methods by name and assigns unique disambiguated names.
    /// Handles overloads by appending numeric suffixes (e.g., "addRedis", "addRedis2").
    /// Filters out property accessors (get_/set_).
    /// </summary>
    protected IEnumerable<MethodOverload> GetMethodOverloads(IEnumerable<RoMethod> methods)
    {
        // Filter out property accessors and group by method name
        var groups = methods
            .Where(m => !m.Name.StartsWith("get_") && !m.Name.StartsWith("set_"))
            .GroupBy(GetMethodBaseName);

        foreach (var group in groups)
        {
            // Sort overloads by parameter count for consistent ordering
            var overloads = group.OrderBy(m => m.Parameters.Count).ToList();

            for (var i = 0; i < overloads.Count; i++)
            {
                var baseName = FormatMethodName(group.Key);
                var uniqueName = i == 0 ? baseName : $"{baseName}{i + 1}";
                yield return new MethodOverload(overloads[i], uniqueName, i);
            }
        }
    }

    /// <summary>
    /// Gets the base method name, checking for PolyglotMethodNameAttribute.
    /// </summary>
    protected virtual string GetMethodBaseName(RoMethod method)
    {
        var attr = method.GetCustomAttributes()
            .FirstOrDefault(a => a.AttributeType.FullName == "Aspire.Hosting.Polyglot.PolyglotMethodNameAttribute");

        if (attr != null)
        {
            var name = attr.NamedArguments.FirstOrDefault(na => na.Key == "MethodName").Value?.ToString()
                ?? attr.FixedArguments?.ElementAtOrDefault(0)?.ToString();
            if (!string.IsNullOrEmpty(name))
            {
                return name;
            }
        }

        return method.Name;
    }

    /// <summary>
    /// Checks if a type is a delegate type (Action, Func, etc.)
    /// </summary>
    protected static bool IsDelegateType(ApplicationModel model, RoType type)
    {
        // Check for Action (no generic args)
        if (type == model.WellKnownTypes.GetKnownType(typeof(Action)))
        {
            return true;
        }

        // Check for generic Action<T>, Action<T1, T2>, etc.
        if (type.IsGenericType)
        {
            var genericDef = type.GenericTypeDefinition;
            var actionTypes = new[]
            {
                typeof(Action<>), typeof(Action<,>), typeof(Action<,,>), typeof(Action<,,,>)
            };
            var funcTypes = new[]
            {
                typeof(Func<>), typeof(Func<,>), typeof(Func<,,>), typeof(Func<,,,>), typeof(Func<,,,,>)
            };

            foreach (var actionType in actionTypes)
            {
                if (genericDef == model.WellKnownTypes.GetKnownType(actionType))
                {
                    return true;
                }
            }

            foreach (var funcType in funcTypes)
            {
                if (genericDef == model.WellKnownTypes.GetKnownType(funcType))
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Checks if a type is a simple/primitive type.
    /// </summary>
    protected static bool IsSimpleType(ApplicationModel model, RoType type)
    {
        var simpleTypes = new[]
        {
            typeof(bool), typeof(char),
            typeof(sbyte), typeof(byte), typeof(short), typeof(ushort),
            typeof(int), typeof(uint), typeof(long), typeof(ulong),
            typeof(nint), typeof(nuint),
            typeof(float), typeof(double), typeof(decimal),
            typeof(string), typeof(Guid), typeof(DateTime), typeof(DateTimeOffset), typeof(TimeSpan)
        };

        foreach (var simpleType in simpleTypes)
        {
            if (type == model.WellKnownTypes.GetKnownType(simpleType))
            {
                return true;
            }
        }

        // Enums are simple types
        if (type.IsEnum)
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Checks if a return type is a primitive type that shouldn't be wrapped.
    /// </summary>
    protected static bool IsPrimitiveReturnType(ApplicationModel model, RoType type)
    {
        // Check for void
        if (type == model.WellKnownTypes.GetKnownType(typeof(void)))
        {
            return true;
        }

        return IsSimpleType(model, type);
    }

    /// <summary>
    /// Checks if a type is a dictionary type (Dictionary, IDictionary).
    /// </summary>
    protected static bool IsDictionaryType(ApplicationModel model, RoType type)
    {
        if (!type.IsGenericType || type.GenericTypeDefinition is not { } genDef)
        {
            return false;
        }

        var dictionaryTypes = new[] { typeof(Dictionary<,>), typeof(IDictionary<,>) };
        return dictionaryTypes.Any(d => genDef == model.WellKnownTypes.GetKnownType(d));
    }

    /// <summary>
    /// Checks if a type is a list/collection type (List, IList, ICollection, IReadOnlyList, IReadOnlyCollection, IEnumerable).
    /// </summary>
    protected static bool IsListType(ApplicationModel model, RoType type)
    {
        if (!type.IsGenericType || type.GenericTypeDefinition is not { } genDef)
        {
            return false;
        }

        var listTypes = new[]
        {
            typeof(List<>), typeof(IList<>), typeof(ICollection<>),
            typeof(IReadOnlyList<>), typeof(IReadOnlyCollection<>), typeof(IEnumerable<>)
        };
        return listTypes.Any(l => genDef == model.WellKnownTypes.GetKnownType(l));
    }

    /// <summary>
    /// Checks if a type is Nullable&lt;T&gt;.
    /// </summary>
    protected static bool IsNullableType(ApplicationModel model, RoType type)
    {
        return type.IsGenericType &&
               type.GenericTypeDefinition == model.WellKnownTypes.GetKnownType(typeof(Nullable<>));
    }

    /// <summary>
    /// Gets the underlying type from Nullable&lt;T&gt;.
    /// </summary>
    protected static RoType? GetNullableUnderlyingType(RoType type)
    {
        if (!type.IsGenericType)
        {
            return null;
        }

        var args = type.GetGenericArguments();
        return args.Count > 0 ? args[0] : null;
    }

    /// <summary>
    /// Checks if a type is IResourceBuilder&lt;T&gt;.
    /// </summary>
    protected static bool IsResourceBuilderType(ApplicationModel model, RoType type)
    {
        return type.IsGenericType &&
               type.GenericTypeDefinition == model.WellKnownTypes.IResourceBuilderType;
    }

    /// <summary>
    /// Gets the resource type T from IResourceBuilder&lt;T&gt;.
    /// </summary>
    protected static RoType? GetResourceBuilderResourceType(RoType type)
    {
        if (!type.IsGenericType)
        {
            return null;
        }

        var args = type.GetGenericArguments();
        return args.Count > 0 ? args[0] : null;
    }

    /// <summary>
    /// Gets the element type from a generic collection (List&lt;T&gt;, IEnumerable&lt;T&gt;, etc.).
    /// </summary>
    protected static RoType? GetCollectionElementType(RoType type)
    {
        if (!type.IsGenericType)
        {
            return null;
        }

        var args = type.GetGenericArguments();
        return args.Count > 0 ? args[0] : null;
    }

    /// <summary>
    /// Converts a .NET type name to a sanitized class name.
    /// </summary>
    protected static string SanitizeClassName(string typeName)
    {
        // Remove generic arity suffix (e.g., "List`1" -> "List")
        var backtickIndex = typeName.IndexOf('`');
        if (backtickIndex > 0)
        {
            typeName = typeName.Substring(0, backtickIndex);
        }

        // Handle nested types (replace '+' with '')
        typeName = typeName.Replace("+", "");

        return typeName;
    }

    /// <summary>
    /// Gets an embedded resource from the calling assembly.
    /// </summary>
    protected static string GetEmbeddedResource(Assembly assembly, string resourceNamespace, string name)
    {
        var resourceName = $"{resourceNamespace}.{name}";
        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{name}' not found.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
