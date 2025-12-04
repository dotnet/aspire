// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

namespace Aspire.Cli.Rosetta.Models.Types;

[DebuggerDisplay("{ToString(),nq}")]
internal class RoDefinitionMethod : RoMethod
{
    private readonly Lazy<RoType> _returnType;
    private readonly MetadataReader _reader;
    private IReadOnlyList<RoType>? _genericArguments;
    private readonly Lazy<List<RoCustomAttributeData>> _customAttributes;
    private readonly List<RoGenericParameterType> _genericParameters;

    public RoDefinitionMethod(MethodDefinitionHandle methodDefinitionHandle, RoType declaringType)
    {
        // Note: assemblyLoaderContext will be used for resolving parameter/return types from other assemblies
        _reader = declaringType.DeclaringAssembly.Reader;

        MethodDefinition = _reader.GetMethodDefinition(methodDefinitionHandle); ;
        DeclaringType = declaringType;

        // Extract method name
        Name = _reader.GetString(MethodDefinition.Name) ?? throw new InvalidOperationException("Invalid method, missing Name.");

        // Extract method attributes
        var attributes = MethodDefinition.Attributes;
        IsStatic = (attributes & MethodAttributes.Static) != 0;

        // Get generic method parameters (if any)
        _genericParameters = [];

        var position = 0;
        foreach (var gpHandle in MethodDefinition.GetGenericParameters())
        {
            _genericParameters.Add(new RoGenericMethodParameterType(this, DeclaringType.DeclaringAssembly, gpHandle, position++));
        }

        IsGenericMethodDefinition = _genericParameters.Count > 0;
        IsGenericMethod = _genericParameters.Count > 0;

        // Load parameters
        Parameters = LoadParameters(MethodDefinition);

        // Initialize lazy-loaded fields
        _returnType = new(LoadReturnType);

        var flags = MethodDefinition.Attributes;
        IsStatic = (flags & MethodAttributes.Static) != 0;

        IsPublic = (flags & MethodAttributes.Public) != 0;

        MetadataToken = MetadataTokens.GetToken(methodDefinitionHandle);

        _customAttributes = new(LoadCustomAttributes);
    }

    public MethodDefinition MethodDefinition { get; }

    // Expose generic parameters as generic arguments for method definitions
    public override IReadOnlyList<RoType> GetGenericArguments() => _genericArguments ??= _genericParameters.Cast<RoType>().ToList();

    private List<RoParameterInfo> LoadParameters(MethodDefinition methodDefinition)
    {
        var parameters = new List<RoParameterInfo>();

        var sig = methodDefinition.DecodeSignature(new DisplayTypeProvider(_reader), genericContext: null);

        var i = 0;
        foreach (var paramHandle in methodDefinition.GetParameters())
        {
            var paramDef = _reader.GetParameter(paramHandle);

            if (paramDef.SequenceNumber == 0)
            {
                continue; // skip return parameter row
            }

            var typeName = sig.ParameterTypes[i];

            var type = DeclaringType.DeclaringAssembly.AssemblyLoaderContext.GetType(typeName, _genericParameters) ?? throw new ArgumentException($"Unknown type: {typeName}");

            var parameter = new RoParameterInfo(paramHandle, paramDef, type, this);
            parameters.Add(parameter);
            i++;
        }

        return parameters;
    }

    public override RoType ReturnType => _returnType.Value;
    public override RoType DeclaringType { get; }
    public override string Name { get; }
    public override IReadOnlyList<RoParameterInfo> Parameters { get; }
    public override bool IsStatic { get; protected set; }
    public override bool IsPublic { get; protected set; }

    /// <summary>
    /// Gets a value indicating whether the current method is a generic method definition.
    /// </summary>
    /// <remarks>A generic method definition is a method whose generic type parameters have not been assigned
    /// specific types. Use this property to determine whether the method must be constructed with specific type
    /// arguments before it can be invoked or examined for its parameters and return type.</remarks>
    public override bool IsGenericMethodDefinition { get; protected set; }

    /// <summary>
    /// Gets or sets a value indicating whether the method is a generic method definition or a constructed generic
    /// method.
    /// </summary>
    public override bool IsGenericMethod { get; protected set; }

    public override int MetadataToken { get; }
    public override RoMethod MakeGenericMethod(params RoType[] typeArguments)
    {
        if (!IsGenericMethodDefinition)
        {
            throw new InvalidOperationException("MakeGenericMethod can only be called on a generic method definition.");
        }

        var genericParamCount = MethodDefinition.GetGenericParameters().Count;
        if (typeArguments.Length != genericParamCount)
        {
            throw new ArgumentException($"The method expects {genericParamCount} generic argument(s) but {typeArguments.Length} were provided.", nameof(typeArguments));
        }

        return new RoConstructedGenericMethod(this, typeArguments);
    }

    public override IEnumerable<RoCustomAttributeData> GetCustomAttributes() => _customAttributes.Value;

    private List<RoCustomAttributeData> LoadCustomAttributes()
    {
        var list = new List<RoCustomAttributeData>();
        var provider = new CustomAttributeTypeProvider(_reader);

        foreach (var customAttributeHandle in MethodDefinition.GetCustomAttributes())
        {
            try
            {
                var customAttribute = _reader.GetCustomAttribute(customAttributeHandle);
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
                            attributeType = DeclaringType.DeclaringAssembly.AssemblyLoaderContext.GetType(fullName);
                            break;
                        }
                    case HandleKind.MemberReference:
                        {
                            var memberRef = _reader.GetMemberReference((MemberReferenceHandle)customAttribute.Constructor);
                            var parent = memberRef.Parent;
                            var fullName = parent.GetTypeName(_reader);

                            if (fullName is not null)
                            {
                                attributeType = DeclaringType.DeclaringAssembly.AssemblyLoaderContext.GetType(fullName);
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
            }
        }

        return list;
    }

    private RoType LoadReturnType()
    {
        var methodSignature = MethodDefinition.DecodeSignature(new DisplayTypeProvider(_reader), null);

        var typeName = methodSignature.ReturnType;

        // The DisplayTypeProvider returns `!!{index}` for generic method parameters and `!{index}` for generic type parameters.
        var type = DeclaringType.DeclaringAssembly.AssemblyLoaderContext.GetType(typeName, _genericParameters) ?? throw new ArgumentException($"Unknown type: {typeName}");

        return type;
    }

}
