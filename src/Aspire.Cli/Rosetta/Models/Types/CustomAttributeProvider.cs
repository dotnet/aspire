// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Reflection.Metadata;

/// <summary>
/// Custom-attribute type provider that returns fully qualified names (string),
/// understands typeof(...) (serialized names), arrays, and enums.
/// For enums: if the enum is defined in *this* module, we read its true underlying
/// primitive from the special 'value__' field; otherwise we assume Int32 (common case).
/// </summary>
internal sealed class CustomAttributeTypeProvider : ICustomAttributeTypeProvider<string>
{
    private readonly MetadataReader _md;
    public CustomAttributeTypeProvider(MetadataReader md) => _md = md;

    // ----- Required type construction -----
    public string GetTypeFromDefinition(MetadataReader r, TypeDefinitionHandle h, byte _) => FullName(r, r.GetTypeDefinition(h));
    public string GetTypeFromReference(MetadataReader r, TypeReferenceHandle h, byte _) => FullName(r, r.GetTypeReference(h));
    public string GetTypeFromSerializedName(string name) => name; // used for typeof(Foo) payloads
    public string GetSZArrayType(string elementType) => elementType + "[]";

    // ----- System.Type handling -----
    public bool IsSystemType(string type) => type == "System.Type";
    public string GetSystemType() => "System.Type";

    // ----- Primitive & enum info for decoding -----
    public PrimitiveTypeCode GetUnderlyingEnumType(string type) => type switch
    {
        "System.Boolean" => PrimitiveTypeCode.Boolean,
        "System.Char" => PrimitiveTypeCode.Char,
        "System.SByte" => PrimitiveTypeCode.SByte,
        "System.Byte" => PrimitiveTypeCode.Byte,
        "System.Int16" => PrimitiveTypeCode.Int16,
        "System.UInt16" => PrimitiveTypeCode.UInt16,
        "System.Int32" => PrimitiveTypeCode.Int32,
        "System.UInt32" => PrimitiveTypeCode.UInt32,
        "System.Int64" => PrimitiveTypeCode.Int64,
        "System.UInt64" => PrimitiveTypeCode.UInt64,
        "System.Single" => PrimitiveTypeCode.Single,
        "System.Double" => PrimitiveTypeCode.Double,
        "System.String" => PrimitiveTypeCode.String,
        "System.IntPtr" => PrimitiveTypeCode.IntPtr,
        "System.UIntPtr" => PrimitiveTypeCode.UIntPtr,
        "System.Object" => PrimitiveTypeCode.Object,
        _ => PrimitiveTypeCode.Int32
    };

    private string FullName(MetadataReader r, TypeDefinition td)
    {
        var ns = r.GetString(td.Namespace);
        var n = r.GetString(td.Name);
        var full = string.IsNullOrEmpty(ns) ? n : $"{ns}.{n}";

        // Build enum info once when we see a TypeDefinition
        // (we need the reader; store minimal facts keyed by full name)
        // Detect enum by base type == System.Enum; find the special "value__" field to get underlying primitive.
        if (!s_enumInfoCache.TryGetValue((r, td), out var enumInfo))
        {
            enumInfo = GetEnumInfo(r, td);
            s_enumInfoCache[(r, td)] = enumInfo;
        }

        if (enumInfo.IsEnum && !s_enum.TryGetValue(full, out _))
        {
            s_enum[full] = (true, enumInfo.UnderlyingPrimitiveName);
        }

        return full;
    }

    private static readonly Dictionary<(MetadataReader, TypeDefinition), (bool IsEnum, string UnderlyingPrimitiveName)> s_enumInfoCache = new();
    private static readonly Dictionary<string, (bool IsEnum, string UnderlyingPrimitiveName)> s_enum = new();

    private (bool IsEnum, string UnderlyingPrimitiveName) GetEnumInfo(MetadataReader md, TypeDefinition td)
    {
        // Base type System.Enum?
        bool baseIsEnum = false;
        var baseType = td.BaseType;
        if (!baseType.IsNil)
        {
            string? baseName = baseType.Kind switch
            {
                HandleKind.TypeReference => FullName(md, md.GetTypeReference((TypeReferenceHandle)baseType)),
                HandleKind.TypeDefinition => FullName(md, md.GetTypeDefinition((TypeDefinitionHandle)baseType)),
                _ => null
            };
            baseIsEnum = baseName == "System.Enum";
        }

        if (!baseIsEnum)
        {
            return (false, "");
        }

        // Find "value__" field and decode its primitive type
        foreach (var fh in td.GetFields())
        {
            var f = md.GetFieldDefinition(fh);
            var name = md.GetString(f.Name);
            if (name != "value__")
            {
                continue;
            }

            var prim = f.DecodeSignature(new FieldPrimProvider(), genericContext: null);
            return (true, GetPrimitiveType(prim));
        }
        // Fallback
        return (true, "System.Int32");
    }

    private static string FullName(MetadataReader r, TypeReference tr)
    {
        var ns = r.GetString(tr.Namespace);
        var n = r.GetString(tr.Name);
        return string.IsNullOrEmpty(ns) ? n : $"{ns}.{n}";
    }

    public string GetPrimitiveType(PrimitiveTypeCode p) => p switch
    {
        PrimitiveTypeCode.Boolean => "System.Boolean",
        PrimitiveTypeCode.Char => "System.Char",
        PrimitiveTypeCode.SByte => "System.SByte",
        PrimitiveTypeCode.Byte => "System.Byte",
        PrimitiveTypeCode.Int16 => "System.Int16",
        PrimitiveTypeCode.UInt16 => "System.UInt16",
        PrimitiveTypeCode.Int32 => "System.Int32",
        PrimitiveTypeCode.UInt32 => "System.UInt32",
        PrimitiveTypeCode.Int64 => "System.Int64",
        PrimitiveTypeCode.UInt64 => "System.UInt64",
        _ => "System.Int32"
    };

    private sealed class FieldPrimProvider : ISignatureTypeProvider<PrimitiveTypeCode, object?>
    {
        public PrimitiveTypeCode GetPrimitiveType(PrimitiveTypeCode typeCode) => typeCode;
        public PrimitiveTypeCode GetTypeFromDefinition(MetadataReader r, TypeDefinitionHandle h, byte _) => PrimitiveTypeCode.Object;
        public PrimitiveTypeCode GetTypeFromReference(MetadataReader r, TypeReferenceHandle h, byte _) => PrimitiveTypeCode.Object;
        public PrimitiveTypeCode GetTypeFromSpecification(MetadataReader r, object? c, TypeSpecificationHandle h, byte _) => PrimitiveTypeCode.Object;
        public PrimitiveTypeCode GetSZArrayType(PrimitiveTypeCode e) => PrimitiveTypeCode.Object;
        public PrimitiveTypeCode GetArrayType(PrimitiveTypeCode e, ArrayShape s) => PrimitiveTypeCode.Object;
        public PrimitiveTypeCode GetPointerType(PrimitiveTypeCode e) => PrimitiveTypeCode.Object;
        public PrimitiveTypeCode GetByReferenceType(PrimitiveTypeCode e) => PrimitiveTypeCode.Object;
        public PrimitiveTypeCode GetPinnedType(PrimitiveTypeCode e) => e;
        public PrimitiveTypeCode GetModifiedType(PrimitiveTypeCode m, PrimitiveTypeCode u, bool isReq) => u;
        public PrimitiveTypeCode GetFunctionPointerType(MethodSignature<PrimitiveTypeCode> s) => PrimitiveTypeCode.Object;
        public PrimitiveTypeCode GetGenericInstantiation(PrimitiveTypeCode g, ImmutableArray<PrimitiveTypeCode> a) => PrimitiveTypeCode.Object;
        public PrimitiveTypeCode GetGenericMethodParameter(object? c, int i) => PrimitiveTypeCode.Object;
        public PrimitiveTypeCode GetGenericTypeParameter(object? c, int i) => PrimitiveTypeCode.Object;
    }
}
