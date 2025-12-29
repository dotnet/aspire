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

    /// <summary>
    /// Tracks all types that are referenced in method signatures and properties.
    /// The code generator can filter by type kind (enum, interface, etc.) as needed.
    /// </summary>
    protected HashSet<RoType> ReferencedTypes { get; } = [];

    /// <summary>
    /// Maps discovered types to their generated class names.
    /// Populated during discovery, used during emission.
    /// </summary>
    protected Dictionary<RoType, string> GeneratedTypeNames { get; } = [];

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
        // === PHASE 1: DISCOVERY ===
        // Walk the model to discover all types that need to be emitted
        DiscoverReferencedTypes(model);

        // === PHASE 2: EMISSION ===
        // 1. Emit header (imports, client setup)
        EmitHeader();

        // 2. Emit createBuilder() function
        EmitCreateBuilderFunction();

        // 3. Emit DistributedApplication class
        EmitDistributedApplicationClass();

        // 4. Emit discovered enum types
        EmitEnumTypes();

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
        // Include both model resources and discovered resource types from GeneratedTypeNames
        var discoveredResourceTypes = GeneratedTypeNames.Keys
            .Where(t => !model.ResourceModels.ContainsKey(t) &&
                        (model.WellKnownTypes.IResourceType.IsAssignableFrom(t) ||
                         model.ResourceModels.ContainsKey(t)));

        var allResources = model.ResourceModels.Values
            .Concat(discoveredResourceTypes.Select(t => new ResourceModel { ResourceType = t }))
            .ToList();

        foreach (var resource in allResources)
        {
            VisitResource(resource);
        }

        // 10. Emit model classes (enums from integrations)
        EmitModelClasses();

        // 11. Emit callback proxy types
        EmitCallbackProxyTypes();

        // 12. Emit any additional classes (parameter classes, etc.)
        EmitAdditionalClasses();
    }

    /// <summary>
    /// Discovery phase: walks the model to find all types that need to be emitted.
    /// This runs before emission so all types are known upfront.
    /// Uses a queue-based approach to avoid stack overflow on deep type graphs.
    /// </summary>
    protected virtual void DiscoverReferencedTypes(ApplicationModel model)
    {
        var visited = new HashSet<RoType>();
        var queue = new Queue<RoType>();

        // Seed the queue with types from proxy properties and methods
        foreach (var (_, proxyModel) in model.BuilderModel.ProxyTypes)
        {
            foreach (var prop in proxyModel.Properties)
            {
                queue.Enqueue(prop.PropertyType);
            }

            foreach (var method in proxyModel.Methods.Concat(proxyModel.StaticMethods))
            {
                EnqueueTypesFromMethod(method, queue);
            }
        }

        // Seed the queue with types from integration methods
        foreach (var integration in model.IntegrationModels.Values)
        {
            foreach (var method in integration.IDistributedApplicationBuilderExtensionMethods)
            {
                EnqueueTypesFromMethod(method, queue);
            }
        }

        // Seed the queue with types from resource methods
        foreach (var resource in model.ResourceModels.Values)
        {
            foreach (var method in resource.IResourceTypeBuilderExtensionsMethods)
            {
                EnqueueTypesFromMethod(method, queue);
            }
        }

        // Process the queue until empty
        while (queue.Count > 0)
        {
            var type = queue.Dequeue();

            // Skip if already visited
            if (!visited.Add(type))
            {
                continue;
            }

            // Skip generic type parameters (e.g., T in IResourceBuilder<T>)
            if (type.IsGenericParameter)
            {
                continue;
            }

            ReferencedTypes.Add(type);

            // Track IResourceBuilder<T> - the T needs a builder class
            if (model.WellKnownTypes.TryGetResourceBuilderTypeArgument(type, out var resourceType))
            {
                if (!resourceType.IsGenericParameter)
                {
                    RegisterGeneratedType(resourceType);
                }
            }

            // Check delegate types (e.g., Action<IResourceBuilder<T>>)
            // For Action<T1, T2, ...>, the generic arguments are the callback parameter types
            if (IsDelegateType(model, type) && type.IsGenericType)
            {
                foreach (var arg in type.GetGenericArguments())
                {
                    // Register callback parameter types for proxy generation
                    // GetGeneratedTypeName will filter what types actually get proxies
                    if (!arg.IsGenericParameter)
                    {
                        RegisterGeneratedType(arg);
                    }

                    // Enqueue for further processing
                    queue.Enqueue(arg);
                }
            }

            // Enqueue generic arguments (includes Nullable<T> underlying type)
            if (type.IsGenericType)
            {
                foreach (var arg in type.GetGenericArguments())
                {
                    queue.Enqueue(arg);
                }
            }
        }
    }

    /// <summary>
    /// Enqueues all types from a method's signature for discovery.
    /// </summary>
    private static void EnqueueTypesFromMethod(RoMethod method, Queue<RoType> queue)
    {
        queue.Enqueue(method.ReturnType);
        foreach (var param in method.Parameters)
        {
            queue.Enqueue(param.ParameterType);
        }
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

    // === Virtual methods for type naming (override in derived classes) ===

    /// <summary>
    /// Gets the generated class name for a .NET type.
    /// Called during discovery to populate GeneratedTypeNames.
    /// Return null if no class should be generated for this type.
    /// </summary>
    protected virtual string? GetGeneratedTypeName(RoType type) => null;

    /// <summary>
    /// Registers a type that needs a generated class.
    /// Calls GetGeneratedTypeName to get the language-specific name.
    /// </summary>
    protected void RegisterGeneratedType(RoType type)
    {
        if (!GeneratedTypeNames.ContainsKey(type))
        {
            var typeName = GetGeneratedTypeName(type);
            if (typeName != null)
            {
                GeneratedTypeNames[type] = typeName;
            }
        }
    }

    // === Virtual methods for language-specific emission (override in derived classes) ===

    /// <summary>Emit file header (imports, client initialization).</summary>
    protected virtual void EmitHeader() { }

    /// <summary>Emit the createBuilder() factory function.</summary>
    protected virtual void EmitCreateBuilderFunction() { }

    /// <summary>Emit the DistributedApplication class.</summary>
    protected virtual void EmitDistributedApplicationClass() { }

    /// <summary>Emit a single enum type.</summary>
    protected virtual void EmitEnumType(RoType enumType) { }

    /// <summary>Emit all discovered enum types (calls EmitEnumType for each).</summary>
    protected virtual void EmitEnumTypes()
    {
        foreach (var type in ReferencedTypes.Where(t => t.IsEnum))
        {
            // Skip if already in ModelTypes (will be generated by EmitModelClasses)
            if (!Model.ModelTypes.Contains(type))
            {
                EmitEnumType(type);
            }
        }
    }

    /// <summary>Emit the builder base class (DistributedApplicationBuilderBase).</summary>
    protected virtual void EmitBuilderBaseClass() { }

    /// <summary>Emit the start of the DistributedApplicationBuilder class.</summary>
    protected virtual void EmitBuilderClassStart() { }

    /// <summary>Emit the end of the DistributedApplicationBuilder class.</summary>
    protected virtual void EmitBuilderClassEnd() { }

    /// <summary>Emit base resource builder classes.</summary>
    protected virtual void EmitBaseResourceBuilderClasses() { }

    /// <summary>Emit model classes (enums from integrations).</summary>
    protected virtual void EmitModelClasses() { }

    /// <summary>Emit a single callback proxy type.</summary>
    protected virtual void EmitCallbackProxyType(RoType proxyType) { }

    /// <summary>Emit all discovered callback proxy types (calls EmitCallbackProxyType for each).</summary>
    protected virtual void EmitCallbackProxyTypes()
    {
        // Callback proxy types are those in GeneratedTypeNames that are NOT resource types
        foreach (var type in GeneratedTypeNames.Keys
            .Where(t => !Model.WellKnownTypes.IResourceType.IsAssignableFrom(t) &&
                        !Model.ResourceModels.ContainsKey(t)))
        {
            EmitCallbackProxyType(type);
        }
    }

    /// <summary>Emit additional classes (parameter classes, etc.).</summary>
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

    /// <summary>
    /// Emit a proxy method (instance or static). Base implementation walks the method
    /// and calls smaller hooks. Override for full control.
    /// </summary>
    protected virtual void EmitProxyMethod(MethodOverload overload, bool isStatic)
    {
        var method = overload.Method;

        // Build parameter contexts
        var parameters = method.Parameters.Select(p => new MethodParameterContext(
            Name: FormatParameterName(p.Name),
            Type: FormatType(p.ParameterType),
            OriginalType: p.ParameterType,
            IsCallback: IsDelegateType(Model, p.ParameterType),
            Original: p
        )).ToList();

        var returnType = method.ReturnType;
        var context = new ProxyMethodContext(
            MethodName: overload.UniqueName,
            OriginalMethodName: method.Name,
            Parameters: parameters,
            ReturnType: FormatType(returnType),
            OriginalReturnType: returnType,
            IsVoid: returnType.Name == "Void",
            IsStatic: isStatic,
            DeclaringType: method.DeclaringType,
            Overload: overload
        );

        EmitProxyMethodStart(context);
        EmitProxyMethodBody(context);
        EmitProxyMethodEnd(context);
    }

    /// <summary>Emit the method signature (async foo(...): Promise&lt;T&gt; {).</summary>
    protected virtual void EmitProxyMethodStart(ProxyMethodContext context) { }

    /// <summary>Emit the method body (the invocation logic).</summary>
    protected virtual void EmitProxyMethodBody(ProxyMethodContext context) { }

    /// <summary>Emit the method end (closing brace).</summary>
    protected virtual void EmitProxyMethodEnd(ProxyMethodContext context) { }

    /// <summary>Emit an extension method (on builder or resource). UniqueName is already disambiguated.</summary>
    protected virtual void EmitExtensionMethod(MethodOverload overload) { }

    /// <summary>Emit a property.</summary>
    protected virtual void EmitProperty(RoPropertyInfo property, PropertyContext context) { }

    /// <summary>Format a type for the target language.</summary>
    protected virtual string FormatType(RoType type) => type.Name;

    /// <summary>Format a method name for the target language (e.g., camelCase, snake_case).</summary>
    protected virtual string FormatMethodName(string name) => name;

    /// <summary>Format a parameter name for the target language.</summary>
    protected virtual string FormatParameterName(string name) => name;

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
    /// Checks if a type is a delegate type (Action, Func, custom delegates, etc.)
    /// </summary>
    protected static bool IsDelegateType(ApplicationModel model, RoType type)
    {
        // For constructed generic types like Func<T, TResult>, check the generic type definition's name
        if (type.IsGenericType && type.GenericTypeDefinition is { } genericDef)
        {
            // Check for Action and Func by name pattern (Action`N or Func`N)
            var name = genericDef.Name;
            if (name.StartsWith("Action`", StringComparison.Ordinal) ||
                name.StartsWith("Func`", StringComparison.Ordinal))
            {
                return true;
            }

            // For other generic delegates, check if the definition inherits from Delegate
            var delegateType = model.WellKnownTypes.GetKnownType(typeof(Delegate));
            return genericDef.IsAssignableTo(delegateType);
        }

        // Non-generic Action
        if (type == model.WellKnownTypes.GetKnownType(typeof(Action)))
        {
            return true;
        }

        // For non-generic types, check inheritance from Delegate
        var delegateBaseType = model.WellKnownTypes.GetKnownType(typeof(Delegate));
        return type.IsAssignableTo(delegateBaseType);
    }

    /// <summary>
    /// Gets the Invoke method for a delegate type.
    /// Returns null for generic Action/Func types (use type arguments instead).
    /// </summary>
    protected static RoMethod? GetDelegateInvokeMethod(RoType delegateType)
    {
        // For generic Action/Func, we can't load the Invoke method from the definition
        // because it has unresolved generic parameter types (!0, !1, etc.)
        // The caller should use GetGenericArguments() instead
        if (delegateType.IsGenericType && delegateType.GenericTypeDefinition is { } genericDef)
        {
            var name = genericDef.Name;
            if (name.StartsWith("Action`", StringComparison.Ordinal) ||
                name.StartsWith("Func`", StringComparison.Ordinal))
            {
                return null;
            }
        }

        // For non-generic delegates or custom delegates, try to get the Invoke method
        return delegateType.GetMethod("Invoke");
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
