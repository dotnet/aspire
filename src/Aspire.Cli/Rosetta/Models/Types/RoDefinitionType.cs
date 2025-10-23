// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Reflection.Metadata;

namespace Aspire.Cli.Rosetta.Models.Types;

internal sealed class RoDefinitionType : RoType
{
    private readonly MetadataReader _reader;
    private readonly Lazy<RoType?> _baseType;
    private readonly Lazy<IReadOnlyList<RoType>> _interfaces;
    private readonly Lazy<bool> _isGenericType;
    private readonly Lazy<bool> _isEnum;
    private readonly Lazy<IReadOnlyList<RoMethod>> _methods;
    private readonly Lazy<IReadOnlyList<RoType>> _genericArguments;
    private readonly Lazy<IReadOnlyList<RoType>> _genericTypeArguments;
    private readonly Lazy<bool> _containsGenericParameters;
    private readonly List<RoGenericParameterType> _genericParameters;
    private readonly Lazy<bool> _isByRef;

    public RoDefinitionType(TypeDefinition typeDefinition, RoAssembly assembly)
        :base(assembly)
    {
        TypeDefinition = typeDefinition;
        _reader = assembly.Reader;

        // Initialize lazy-loaded fields
        _baseType = new(LoadBaseType);
        _interfaces = new(LoadInterfaces);
        _isGenericType = new(LoadIsGenericType);
        _isEnum = new(LoadIsEnum);
        _methods = new(LoadMethods);
        _genericArguments = new(LoadGenericArguments);
        _genericTypeArguments = new(LoadGenericTypeArguments);
        _containsGenericParameters = new(LoadContainsGenericParameters);
        _isByRef = new(LoadIsByRef);

        // Extract basic type information
        Name = _reader.GetString(typeDefinition.Name) ?? throw new InvalidOperationException("Invalid type, missing Name.");

        if (typeDefinition.IsNested)
        {
            var enclosingTypeHandle = typeDefinition.GetDeclaringType();
            var enclosingType = _reader.GetTypeDefinition(enclosingTypeHandle);
            var enclosingTypeName = _reader.GetString(enclosingType.Name) ?? throw new InvalidOperationException("Invalid enclosing type, missing Name.");

            Name = $"{enclosingTypeName}+{Name}";

            var namespaceName = enclosingType.Namespace.IsNil ? string.Empty : _reader.GetString(enclosingType.Namespace);
            FullName = string.IsNullOrEmpty(namespaceName) ? Name : $"{namespaceName}.{Name}";
        }
        else
        {
            var namespaceName = typeDefinition.Namespace.IsNil ? string.Empty : _reader.GetString(typeDefinition.Namespace);
            FullName = string.IsNullOrEmpty(namespaceName) ? Name : $"{namespaceName}.{Name}";
        }

        // Get generic method parameters (if any)
        _genericParameters = [];

        var position = 0;
        foreach (var gpHandle in typeDefinition.GetGenericParameters())
        {
            _genericParameters.Add(new RoGenericTypeParameterType(this, gpHandle, position++));
        }

        // Extract type attributes
        var attributes = typeDefinition.Attributes;
        var visibility = attributes & TypeAttributes.VisibilityMask;
        IsPublic = visibility == TypeAttributes.Public ||
                  visibility == TypeAttributes.NestedPublic;
        IsAbstract = (attributes & TypeAttributes.Abstract) != 0;
        IsSealed = (attributes & TypeAttributes.Sealed) != 0;
        IsInterface = (attributes & TypeAttributes.Interface) != 0;
        IsNested = typeDefinition.IsNested;
        IsTypeDefinition = true;

        // Simple properties that don't require complex resolution
        IsGenericParameter = false;

        // If this RoType represents a generic type definition (has generic parameters), its generic type definition is itself.
        GenericTypeDefinition = TypeDefinition.GetGenericParameters().Count > 0 ? this : null;
    }

    public override string Name { get; }
    public override string FullName { get; }
    public TypeDefinition TypeDefinition { get; }
    public override bool IsEnum => _isEnum.Value;
    public override bool IsByRef => _isByRef.Value;
    public override bool IsGenericType => _isGenericType.Value;

    public override IReadOnlyList<RoType> GetGenericArguments() => _genericArguments.Value;
    public override bool ContainsGenericParameters => _containsGenericParameters.Value;
    public override IReadOnlyList<RoType> Interfaces => _interfaces.Value;
    public override RoType? BaseType => _baseType.Value;
    public override IReadOnlyList<RoType> GenericTypeArguments => _genericTypeArguments.Value;
    public override RoType MakeGenericType(params RoType[] typeArguments)
    {
        return new RoConstructedGenericType(this, typeArguments);
    }
    public override IReadOnlyList<RoMethod> Methods => _methods.Value;
    public override RoMethod? GetMethod(string name)
    {
        return Methods.FirstOrDefault(m => m.Name == name);
    }
    public override IEnumerable<RoCustomAttributeData> GetCustomAttributes()
    {
        var list = new List<RoCustomAttributeData>();
        var provider = new CustomAttributeTypeProvider(_reader);

        foreach (var attrHandle in TypeDefinition.GetCustomAttributes())
        {
            try
            {
                var customAttribute = _reader.GetCustomAttribute(attrHandle);
                RoType? attributeType = null;

                switch (customAttribute.Constructor.Kind)
                {
                    case HandleKind.MethodDefinition:
                        {
                            var ctorMethod = _reader.GetMethodDefinition((MethodDefinitionHandle)customAttribute.Constructor);
                            var declaringTypeHandle = ctorMethod.GetDeclaringType();
                            var typeDef = _reader.GetTypeDefinition(declaringTypeHandle);
                            var name = _reader.GetString(typeDef.Name);
                            var ns = typeDef.Namespace.IsNil ? string.Empty : _reader.GetString(typeDef.Namespace);
                            var fullName = string.IsNullOrEmpty(ns) ? name : $"{ns}.{name}";
                            attributeType = DeclaringAssembly.AssemblyLoaderContext.GetType(fullName);
                            break;
                        }
                    case HandleKind.MemberReference:
                        {
                            var memberRef = _reader.GetMemberReference((MemberReferenceHandle)customAttribute.Constructor);
                            var parent = memberRef.Parent;
                            var fullName = parent.GetTypeName(_reader);

                            if (fullName is not null)
                            {
                                attributeType = DeclaringAssembly.AssemblyLoaderContext.GetType(fullName);
                            }
                            break;
                        }
                }

                var val = customAttribute.DecodeValue(provider);

                var fixedArgs = val.FixedArguments.Select(a => a.Value).ToArray();
                var namedArgs = val.NamedArguments.Select(na => new KeyValuePair<string, object>(na.Name!, na.Value!)).ToArray(); //_reader.GetString(na.Name),

                if (attributeType is not null)
                {
                    list.Add(new RoCustomAttributeData
                    {
                        AttributeType = attributeType,
                        FixedArguments = fixedArgs,
                        NamedArguments = namedArgs
                    });
                }
            }
            catch
            {
                // Skip attributes that can't be resolved
            }
        }

        return list;
    }

    public override IEnumerable<string> GetEnumNames()
    {
        if (!IsEnum)
        {
            return Enumerable.Empty<string>();
        }

        var enumNames = new List<string>();

        // Enumerate through all fields in the type definition
        foreach (var fieldHandle in TypeDefinition.GetFields())
        {
            var field = _reader.GetFieldDefinition(fieldHandle);

            // Enum values are represented as fields with the Literal attribute
            // (static, literal fields that hold the enum values)
            if ((field.Attributes & FieldAttributes.Literal) != 0)
            {
                var fieldName = _reader.GetString(field.Name);
                enumNames.Add(fieldName);
            }
        }

        return enumNames;
    }

    private bool LoadIsByRef()
    {
        return GetCustomAttributes().Any(attr => attr.AttributeType.FullName == "System.Runtime.CompilerServices.IsByRefLikeAttribute");
    }

    private RoType? LoadBaseType()
    {
        var baseTypeHandle = TypeDefinition.BaseType;

        // If there's no base type, return null (e.g., System.Object or interfaces)
        if (baseTypeHandle.IsNil)
        {
            return null;
        }

        if (!AssemblyLoaderContext.TryGetFullName(baseTypeHandle, _reader, out var baseTypeFullName))
        {
            return null; // Unable to resolve base type
        }

        return DeclaringAssembly.AssemblyLoaderContext.GetType(baseTypeFullName, _genericParameters);
    }

    private List<RoType> LoadInterfaces()
    {
        var interfaceHandles = TypeDefinition.GetInterfaceImplementations();
        if (interfaceHandles.Count == 0)
        {
            return [];
        }

        var interfaces = new List<RoType>(interfaceHandles.Count);

        foreach (var implementationHandle in interfaceHandles)
        {
            var implementation = _reader.GetInterfaceImplementation(implementationHandle);

            if (!AssemblyLoaderContext.TryGetFullName(implementation.Interface, _reader, out var fullName))
            {
                continue;
            }

            var interfaceType = DeclaringAssembly.AssemblyLoaderContext.GetType(fullName, _genericParameters);

            if (interfaceType is not null)
            {
                interfaces.Add(interfaceType);
            }
        }

        return interfaces;
    }

    private bool LoadIsGenericType()
    {
        // Check if the type has generic parameters
        var genericParams = TypeDefinition.GetGenericParameters();
        return genericParams.Count > 0;
    }

    private bool LoadIsEnum()
    {
        // Check if the base type is System.Enum
        var baseType = BaseType;
        return baseType?.FullName == "System.Enum";
    }

    private List<RoDefinitionMethod> LoadMethods()
    {
        var methods = new List<RoDefinitionMethod>();

        // Memoize property accessor so we can skip them

        var accessorHandles = TypeDefinition.GetProperties()
            .Select(ph => _reader.GetPropertyDefinition(ph).GetAccessors())
            .SelectMany(a => new[] { a.Getter, a.Setter }.Where(h => !h.IsNil))
            .ToHashSet();

        foreach (var methodHandle in TypeDefinition.GetMethods())
        {
            var methodDefinition = _reader.GetMethodDefinition(methodHandle);

            // Extract method attributes to filter
            var attributes = methodDefinition.Attributes;
            var memberAccess = attributes & MethodAttributes.MemberAccessMask;
            var isPublic = memberAccess == MethodAttributes.Public;

            // Skip non-public methods for now (could be configurable later)
            if (!isPublic)
            {
                continue;
            }

            if (accessorHandles.Contains(methodHandle))
            {
                continue; // skip property accessors
            }

            // Create RoMethodInfo instance - all metadata extraction happens in constructor
            var method = new RoDefinitionMethod(methodHandle, this);
            methods.Add(method);
        }

        return methods;
    }

    private IReadOnlyList<RoType> LoadGenericArguments()
    {
        return _genericParameters;
    }

    private IReadOnlyList<RoType> LoadGenericTypeArguments()
    {
        return [];
    }

    private bool LoadContainsGenericParameters()
    {
        // Check if this type or any of its generic arguments contain unresolved generic parameters
        if (IsGenericType)
        {
            return true;
        }
        return false;
    }

    public override string ToString()
    {
        return FullName;
    }
}
