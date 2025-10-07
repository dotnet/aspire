// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Reflection.Metadata;

namespace Aspire.Cli.Rosetta.Models.Types;

internal static class SrmTypeShape
{
    // ---------- Public helpers over EntityHandle ----------

    public static bool IsArrayType(MetadataReader md, EntityHandle type)
        => GetKind(md, type) == TypeKind.Array;

    public static bool IsByRefType(MetadataReader md, EntityHandle type)
        => GetKind(md, type) == TypeKind.ByRef;

    public static bool IsPointerType(MetadataReader md, EntityHandle type)
        => GetKind(md, type) == TypeKind.Pointer;

    // ---------- Optional: helpers for MethodDefinition ----------

    /// <summary>Return true if the method's *return type* is byref (ref-return).</summary>
    public static bool IsReturnByRef(MetadataReader md, MethodDefinition method)
    {
        var sig = method.DecodeSignature(new ShapeProvider(md), genericContext: null);
        return sig.ReturnType.Kind == TypeKind.ByRef;
    }

    /// <summary>
    /// Determines whether the specified method returns an array by reference.
    /// </summary>
    public static bool IsReturnArray(MetadataReader md, MethodDefinition method)
    {
        var sig = method.DecodeSignature(new ShapeProvider(md), genericContext: null);
        return sig.ReturnType.Kind == TypeKind.Array;
    }

    /// <summary>Return true if the method's parameter at zero-based index is byref.</summary>
    public static bool IsParameterByRef(MetadataReader md, MethodDefinition method, int parameterIndex)
    {
        var sig = method.DecodeSignature(new ShapeProvider(md), genericContext: null);
        return parameterIndex >= 0
            && parameterIndex < sig.ParameterTypes.Length
            && sig.ParameterTypes[parameterIndex].Kind == TypeKind.ByRef;
    }

    /// <summary>Return true if the method's parameter at zero-based index is a pointer type.</summary>
    public static bool IsParameterPointer(MetadataReader md, MethodDefinition method, int parameterIndex)
    {
        var sig = method.DecodeSignature(new ShapeProvider(md), genericContext: null);
        return parameterIndex >= 0
            && parameterIndex < sig.ParameterTypes.Length
            && sig.ParameterTypes[parameterIndex].Kind == TypeKind.Pointer;
    }

    /// <summary>Return true if the method's parameter at zero-based index is an array type.</summary>
    public static bool IsParameterArray(MetadataReader md, MethodDefinition method, int parameterIndex)
    {
        var sig = method.DecodeSignature(new ShapeProvider(md), genericContext: null);
        return parameterIndex >= 0
            && parameterIndex < sig.ParameterTypes.Length
            && sig.ParameterTypes[parameterIndex].Kind == TypeKind.Array;
    }

    // ---------- Core: map a handle to a coarse "kind" ----------

    private static TypeKind GetKind(MetadataReader md, EntityHandle h) => h.Kind switch
    {
        // Declared and referenced types are never byref/pointer/array on their own.
        HandleKind.TypeDefinition => TypeKind.Named,
        HandleKind.TypeReference => TypeKind.Named,

        // Constructed types (arrays, pointers, byrefs, generics, etc.) live in TypeSpec.
        HandleKind.TypeSpecification => md.GetTypeSpecification((TypeSpecificationHandle)h)
                                        .DecodeSignature(new ShapeProvider(md), genericContext: null)
                                        .Kind,

        _ => TypeKind.Unsupported
    };

    // ---------- Shape model + provider ----------

    public enum TypeKind { Named, Array, Pointer, ByRef, GenericInstance, FunctionPointer, Modified, Unsupported }

    public readonly struct TypeShapeNode
    {
        public TypeKind Kind { get; }
        public TypeShapeNode(TypeKind kind) => Kind = kind;

        public static readonly TypeShapeNode Named = new(TypeKind.Named);
        public static readonly TypeShapeNode Array = new(TypeKind.Array);
        public static readonly TypeShapeNode Pointer = new(TypeKind.Pointer);
        public static readonly TypeShapeNode ByRef = new(TypeKind.ByRef);
        public static readonly TypeShapeNode GenInst = new(TypeKind.GenericInstance);
        public static readonly TypeShapeNode FnPtr = new(TypeKind.FunctionPointer);
        public static readonly TypeShapeNode Modified = new(TypeKind.Modified);
        public static readonly TypeShapeNode Bad = new(TypeKind.Unsupported);
    }

    internal sealed class ShapeProvider : ISignatureTypeProvider<TypeShapeNode, object?>
    {
        private readonly MetadataReader _md;
        public ShapeProvider(MetadataReader md) => _md = md;

        public TypeShapeNode GetSZArrayType(TypeShapeNode elementType) => TypeShapeNode.Array;
        public TypeShapeNode GetArrayType(TypeShapeNode elementType, ArrayShape shape) => TypeShapeNode.Array;
        public TypeShapeNode GetPointerType(TypeShapeNode elementType) => TypeShapeNode.Pointer;
        public TypeShapeNode GetByReferenceType(TypeShapeNode elementType) => TypeShapeNode.ByRef;
        public TypeShapeNode GetPinnedType(TypeShapeNode elementType) => elementType;

        // Modern SRM exposes GetModifiedType; keep Optional/Required delegating to it.
        public TypeShapeNode GetModifiedType(TypeShapeNode modifier, TypeShapeNode unmodifiedType, bool isRequired)
            => TypeShapeNode.Modified;
        public TypeShapeNode GetOptionalModifier(TypeShapeNode modifier, TypeShapeNode unmodifiedType)
            => GetModifiedType(modifier, unmodifiedType, isRequired: false);
        public TypeShapeNode GetRequiredModifier(TypeShapeNode modifier, TypeShapeNode unmodifiedType)
            => GetModifiedType(modifier, unmodifiedType, isRequired: true);

        public TypeShapeNode GetFunctionPointerType(MethodSignature<TypeShapeNode> signature) => TypeShapeNode.FnPtr;

        public TypeShapeNode GetGenericInstantiation(TypeShapeNode genericType, ImmutableArray<TypeShapeNode> typeArguments)
            => TypeShapeNode.GenInst;

        public TypeShapeNode GetGenericMethodParameter(object? genericContext, int index) => TypeShapeNode.Named;
        public TypeShapeNode GetGenericTypeParameter(object? genericContext, int index) => TypeShapeNode.Named;

        public TypeShapeNode GetPrimitiveType(PrimitiveTypeCode typeCode) => TypeShapeNode.Named;

        public TypeShapeNode GetTypeFromDefinition(MetadataReader r, TypeDefinitionHandle h, byte rawTypeKind) => TypeShapeNode.Named;
        public TypeShapeNode GetTypeFromReference(MetadataReader r, TypeReferenceHandle h, byte rawTypeKind) => TypeShapeNode.Named;
        public TypeShapeNode GetTypeFromSpecification(MetadataReader r, object? ctx, TypeSpecificationHandle h, byte rawTypeKind)
            => r.GetTypeSpecification(h).DecodeSignature(this, ctx);
    }

    /// <summary>
    /// Tries to read a parameter's default value from the Constant table.
    /// Returns true if a default exists; 'value' is the decoded object and 'raw' are the original blob bytes.
    /// </summary>
    public static bool TryGetParameterDefaultValue(
        MetadataReader md,
        Parameter parameter,
        out object? value)
    {
        value = null;

        var constHandle = parameter.GetDefaultValue();
        if (constHandle.IsNil)
        {
            // TODO: In this case MetadataLoadContext looks for a custom attribute
            // c.f. https://source.dot.net/#System.Reflection.MetadataLoadContext/System/Reflection/TypeLoading/Parameters/Ecma/EcmaFatMethodParameter.cs,62

            return false;
        }

        var constant = md.GetConstant(constHandle);

        // Decode (DefaultValue)
        var br = md.GetBlobReader(constant.Value); // fresh reader for decoding
        value = constant.TypeCode switch
        {
            ConstantTypeCode.Boolean => br.ReadBoolean(),
            ConstantTypeCode.Char => br.ReadChar(),
            ConstantTypeCode.SByte => br.ReadSByte(),
            ConstantTypeCode.Byte => br.ReadByte(),
            ConstantTypeCode.Int16 => br.ReadInt16(),
            ConstantTypeCode.UInt16 => br.ReadUInt16(),
            ConstantTypeCode.Int32 => br.ReadInt32(),
            ConstantTypeCode.UInt32 => br.ReadUInt32(),
            ConstantTypeCode.Int64 => br.ReadInt64(),
            ConstantTypeCode.UInt64 => br.ReadUInt64(),
            ConstantTypeCode.Single => br.ReadSingle(),
            ConstantTypeCode.Double => br.ReadDouble(),
            ConstantTypeCode.String => br.ReadUTF16(br.Length), // UTF-16 serialized string (can be null)
            ConstantTypeCode.NullReference => null,
            _ => null
        };

        return true;
    }

    /// <summary>
    /// Convenience: get default for the Nth parameter of a method (0-based by signature order).
    /// Skips the "return parameter" (SequenceNumber == 0).
    /// </summary>
    public static bool TryGetParameterDefaultValue(
        MetadataReader md,
        MethodDefinitionHandle methodHandle,
        int parameterIndex,
        out object? value)
    {
        value = null;

        var mdef = md.GetMethodDefinition(methodHandle);
        var handles = mdef.GetParameters();

        // Map signature index -> ParameterHandle via SequenceNumber
        var count = 0;
        foreach (var ph in handles)
        {
            var p = md.GetParameter(ph);
            if (p.SequenceNumber == 0)
            {
                continue; // return parameter
            }

            if (count == parameterIndex)
            {
                return TryGetParameterDefaultValue(md, p, out value);
            }

            count++;
        }
        return false; // not found / index out of range
    }
}
