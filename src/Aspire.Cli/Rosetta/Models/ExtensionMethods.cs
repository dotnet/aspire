// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection.Metadata;

namespace Aspire.Cli.Rosetta.Models;

internal static class ExtensionMethods
{
    public static string GetTypeName(this EntityHandle handle, MetadataReader md)
    {
        switch (handle.Kind)
        {
            case HandleKind.TypeDefinition:
                {
                    var td = md.GetTypeDefinition((TypeDefinitionHandle)handle);
                    var ns = md.GetString(td.Namespace);
                    var name = md.GetString(td.Name);
                    return string.IsNullOrEmpty(ns) ? name : $"{ns}.{name}";
                }
            case HandleKind.TypeReference:
                {
                    var tr = md.GetTypeReference((TypeReferenceHandle)handle);
                    var ns = md.GetString(tr.Namespace);
                    var name = md.GetString(tr.Name);
                    return string.IsNullOrEmpty(ns) ? name : $"{ns}.{name}";
                }
            case HandleKind.TypeSpecification:
                {
                    // For generics/arrays/pointers/etc. we'll decode via signatures elsewhere.
                    return "<type-spec>";
                }
            default:
                return "<unknown-type>";
        }
    }
}
