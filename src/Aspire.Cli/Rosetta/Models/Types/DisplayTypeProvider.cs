// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Reflection.Metadata;

namespace Aspire.Cli.Rosetta.Models.Types;

internal sealed class DisplayTypeProvider : ISignatureTypeProvider<string, object?>
{
    private readonly MetadataReader _md;
    public DisplayTypeProvider(MetadataReader md) => _md = md;

    public string GetArrayType(string elementType, ArrayShape shape)
        => $"{elementType}[{new string(',', shape.Rank - 1)}]";

    public string GetByReferenceType(string elementType) => $"{elementType}&";
    public string GetPointerType(string elementType) => $"{elementType}*";
    public string GetSZArrayType(string elementType) => $"{elementType}[]";
    public string GetPinnedType(string elementType) => elementType;

    public string GetFunctionPointerType(MethodSignature<string> signature)
    {
        var parms = string.Join(", ", signature.ParameterTypes);
        return $"delegate*<{parms}{(parms.Length > 0 ? ", " : "")}{signature.ReturnType}>";
    }

    public string GetGenericInstantiation(string genericType, ImmutableArray<string> typeArguments)
        => $"{genericType}<{string.Join(", ", typeArguments)}>";

    /// <summary>
    /// Returns a string representation of a generic method parameter at the specified index.
    /// e.g., T for Method&lt;T&gt;
    /// </summary>
    public string GetGenericMethodParameter(object? genericContext, int index) => $"!!{index}";

    /// <summary>
    /// Returns a string representation of a generic type parameter at the specified index.
    /// e.g., T for Type&lt;T&gt;
    /// </summary>
    public string GetGenericTypeParameter(object? genericContext, int index) => $"!{index}";

    public string GetModifiedType(string modifier, string unmodifiedType, bool isRequired)
        => unmodifiedType; // ignore custom modifiers for display

    public string GetOptionalModifier(string modifier, string unmodifiedType)
        => GetModifiedType(modifier, unmodifiedType, isRequired: false);

    public string GetRequiredModifier(string modifier, string unmodifiedType)
        => GetModifiedType(modifier, unmodifiedType, isRequired: true);

    public string GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte _)
        => GetFullName(reader.GetTypeDefinition(handle));

    public string GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte _)
        => GetFullName(reader.GetTypeReference(handle));

    public string GetTypeFromSpecification(MetadataReader reader, object? genericContext, TypeSpecificationHandle handle, byte _)
        => reader.GetTypeSpecification(handle).DecodeSignature(this, genericContext);

    public string GetPrimitiveType(PrimitiveTypeCode typeCode) => typeCode switch
    {
        PrimitiveTypeCode.Void => typeof(void).FullName!,
        PrimitiveTypeCode.Boolean => typeof(bool).FullName!,
        PrimitiveTypeCode.Char => typeof(char).FullName!,
        PrimitiveTypeCode.SByte => typeof(sbyte).FullName!,
        PrimitiveTypeCode.Byte => typeof(byte).FullName!,
        PrimitiveTypeCode.Int16 => typeof(short).FullName!,
        PrimitiveTypeCode.UInt16 => typeof(ushort).FullName!,
        PrimitiveTypeCode.Int32 => typeof(int).FullName!,
        PrimitiveTypeCode.UInt32 => typeof(uint).FullName!,
        PrimitiveTypeCode.Int64 => typeof(long).FullName!,
        PrimitiveTypeCode.UInt64 => typeof(ulong).FullName!,
        PrimitiveTypeCode.Single => typeof(float).FullName!,
        PrimitiveTypeCode.Double => typeof(double).FullName!,
        PrimitiveTypeCode.String => typeof(string).FullName!,
        PrimitiveTypeCode.IntPtr => typeof(nint).FullName!,
        PrimitiveTypeCode.UIntPtr => typeof(nuint).FullName!,
        PrimitiveTypeCode.Object => typeof(object).FullName!,
        _ => typeCode.ToString()
    };

    public string GetPrimitiveType(PrimitiveTypeCode typeCode, bool _) => GetPrimitiveType(typeCode);

    public string GetTypeFromHandle(EntityHandle handle, bool? _ = null) => handle.Kind switch
    {
        HandleKind.TypeDefinition => GetTypeFromDefinition(_md, (TypeDefinitionHandle)handle, 0),
        HandleKind.TypeReference => GetTypeFromReference(_md, (TypeReferenceHandle)handle, 0),
        HandleKind.TypeSpecification => GetTypeFromSpecification(_md, null, (TypeSpecificationHandle)handle, 0),
        _ => "<unknown>"
    };

    private string GetFullName(TypeDefinition td)
    {
        var name = _md.GetString(td.Name);
        
        // Handle nested types by walking up the declaring type chain
        if (td.IsNested)
        {
            var declaringType = _md.GetTypeDefinition(td.GetDeclaringType());
            var declaringTypeName = GetFullName(declaringType);
            return $"{declaringTypeName}+{name}";
        }
        
        var ns = _md.GetString(td.Namespace);
        return string.IsNullOrEmpty(ns) ? name : $"{ns}.{name}";
    }

    private string GetFullName(TypeReference tr)
    {
        var name = _md.GetString(tr.Name);
        
        // Handle nested types
        if (tr.ResolutionScope.Kind == HandleKind.TypeReference)
        {
            var declaringType = _md.GetTypeReference((TypeReferenceHandle)tr.ResolutionScope);
            var declaringTypeName = GetFullName(declaringType);
            return $"{declaringTypeName}+{name}";
        }
        
        var ns = _md.GetString(tr.Namespace);
        return string.IsNullOrEmpty(ns) ? name : $"{ns}.{name}";
    }

    // ISignatureTypeProvider requires these, routed above:
    string ISimpleTypeProvider<string>.GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind)
        => GetTypeFromDefinition(reader, handle, rawTypeKind);
    string ISimpleTypeProvider<string>.GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind)
        => GetTypeFromReference(reader, handle, rawTypeKind);
    string ISignatureTypeProvider<string, object?>.GetTypeFromSpecification(MetadataReader reader, object? genericContext, TypeSpecificationHandle handle, byte rawTypeKind)
        => GetTypeFromSpecification(reader, genericContext, handle, rawTypeKind);
}
