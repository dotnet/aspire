// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Reflection.Metadata;
using static Aspire.Cli.Rosetta.Models.Types.SrmTypeShape;

namespace Aspire.Cli.Rosetta.Models.Types;

internal sealed class RoParameterInfo
{
    private readonly ParameterHandle _parameterHandle;
    private readonly Parameter _parameter;
    private readonly MetadataReader _reader;
    private readonly AssemblyLoaderContext _assemblyLoaderContext;
    private readonly Lazy<List<RoCustomAttributeData>> _customAttributes;

    public RoParameterInfo(ParameterHandle parameterHandle, Parameter parameter, RoType parameterType, RoMethod declaringMethod)
    {
        _parameterHandle = parameterHandle;
        _parameter = parameter;
        _reader = declaringMethod.DeclaringType.DeclaringAssembly.Reader;
        _assemblyLoaderContext = declaringMethod.DeclaringType.DeclaringAssembly.AssemblyLoaderContext;

        ParameterType = parameterType;

        DeclaringMethod = declaringMethod;

        // Extract parameter name
        Name = parameter.Name.IsNil ? string.Empty : _reader.GetString(parameter.Name);

        IsOptional = (_parameter.Attributes & ParameterAttributes.Optional) != 0;

        if (TryGetParameterDefaultValue(_reader, parameter, out var defaultValue))
        {
            RawDefaultValue = defaultValue;
        }
        else
        {
            RawDefaultValue = IsOptional ? Missing.Value : DBNull.Value;
        }

        _customAttributes = new(LoadCustomAttributes);
    }

    public RoMethod DeclaringMethod { get; }
    public RoType ParameterType { get; }
    public string Name { get; }

    /// <summary>
    /// Gets a value indicating whether the item can be omitted during in a method call.
    /// </summary>
    public bool IsOptional { get; }

    /// <summary>
    /// Gets a value indicating the default value if the parameter has a default value.
    /// This property can be used in the reflection-only (RO) context since it won't depend on runtime values from
    /// other assemblies.
    /// </summary>
    public object? RawDefaultValue { get; }

    public IEnumerable<RoCustomAttributeData> GetCustomAttributes() => _customAttributes.Value;

    private List<RoCustomAttributeData> LoadCustomAttributes()
    {
        var list = new List<RoCustomAttributeData>();
        var provider = new CustomAttributeTypeProvider(_reader);

        try
        {
            foreach (var attrHandle in _reader.GetCustomAttributes(_parameterHandle))
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
                                attributeType = DeclaringMethod.DeclaringType.DeclaringAssembly.GetType(fullName);
                                break;
                            }
                        case HandleKind.MemberReference:
                            {
                                var memberRef = _reader.GetMemberReference((MemberReferenceHandle)customAttribute.Constructor);
                                var parent = memberRef.Parent;
                                var fullName = parent.GetTypeName(_reader);

                                if (fullName is not null)
                                {
                                    attributeType = DeclaringMethod.DeclaringType.DeclaringAssembly.GetType(fullName);
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
                    // Ignore malformed attribute rows
                }
            }
        }
        catch
        {
        }

        return list;
    }

    public override string ToString()
    {
        return $"{Name}: {ParameterType}";
    }
}
