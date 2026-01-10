// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Text.RegularExpressions;
using Aspire.Hosting.Analyzers.Infrastructure;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Aspire.Hosting.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public partial class AspireExportAnalyzer : DiagnosticAnalyzer
{
    // Matches: valid method name (camelCase identifier, may contain dots for namespacing)
    // Examples: addRedis, addContainer, Dictionary.set
    private static readonly Regex s_exportIdPattern = new(
        @"^[a-zA-Z][a-zA-Z0-9.]*$",
        RegexOptions.Compiled);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => Diagnostics.SupportedDiagnostics;

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterCompilationStartAction(AnalyzeCompilationStart);
    }

    private void AnalyzeCompilationStart(CompilationStartAnalysisContext context)
    {
        var wellKnownTypes = WellKnownTypes.GetOrCreate(context.Compilation);

        // Try to get the AspireExportAttribute type - if it doesn't exist, nothing to analyze
        INamedTypeSymbol? aspireExportAttribute;
        try
        {
            aspireExportAttribute = wellKnownTypes.Get(WellKnownTypeData.WellKnownType.Aspire_Hosting_AspireExportAttribute);
        }
        catch (InvalidOperationException)
        {
            // Type not found in compilation, nothing to analyze
            return;
        }

        context.RegisterSymbolAction(
            c => AnalyzeMethod(c, wellKnownTypes, aspireExportAttribute),
            SymbolKind.Method);
    }

    private static void AnalyzeMethod(
        SymbolAnalysisContext context,
        WellKnownTypes wellKnownTypes,
        INamedTypeSymbol aspireExportAttribute)
    {
        var method = (IMethodSymbol)context.Symbol;

        // Find AspireExportAttribute on the method
        AttributeData? exportAttribute = null;
        foreach (var attr in method.GetAttributes())
        {
            if (SymbolEqualityComparer.Default.Equals(attr.AttributeClass, aspireExportAttribute))
            {
                exportAttribute = attr;
                break;
            }
        }

        if (exportAttribute is null)
        {
            return;
        }

        var attributeSyntax = exportAttribute.ApplicationSyntaxReference?.GetSyntax(context.CancellationToken);
        var location = attributeSyntax?.GetLocation() ?? method.Locations.FirstOrDefault() ?? Location.None;

        // Rule 1: Method must be static
        if (!method.IsStatic)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Diagnostics.s_exportMethodMustBeStatic,
                location,
                method.Name));
        }

        // Rule 2: Validate export ID format
        var exportId = GetExportId(exportAttribute);
        if (exportId is not null && !s_exportIdPattern.IsMatch(exportId))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Diagnostics.s_invalidExportIdFormat,
                location,
                exportId));
        }

        // Rule 3: Validate return type is ATS-compatible
        if (!IsAtsCompatibleType(method.ReturnType, wellKnownTypes, aspireExportAttribute))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Diagnostics.s_returnTypeMustBeAtsCompatible,
                location,
                method.Name,
                method.ReturnType.ToDisplayString()));
        }

        // Rule 4: Validate parameter types are ATS-compatible
        foreach (var parameter in method.Parameters)
        {
            if (!IsAtsCompatibleParameter(parameter, wellKnownTypes, aspireExportAttribute))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    Diagnostics.s_parameterTypeMustBeAtsCompatible,
                    location,
                    parameter.Name,
                    parameter.Type.ToDisplayString(),
                    method.Name));
            }
        }
    }

    private static string? GetExportId(AttributeData attribute)
    {
        if (attribute.ConstructorArguments.Length > 0 &&
            attribute.ConstructorArguments[0].Value is string id)
        {
            return id;
        }
        return null;
    }

    private static bool IsAtsCompatibleType(
        ITypeSymbol type,
        WellKnownTypes wellKnownTypes,
        INamedTypeSymbol aspireExportAttribute)
    {
        // void is allowed
        if (type.SpecialType == SpecialType.System_Void)
        {
            return true;
        }

        // Task and Task<T> are allowed (for async methods)
        if (IsTaskType(type, wellKnownTypes, aspireExportAttribute))
        {
            return true;
        }

        return IsAtsCompatibleValueType(type, wellKnownTypes, aspireExportAttribute);
    }

    private static bool IsTaskType(
        ITypeSymbol type,
        WellKnownTypes wellKnownTypes,
        INamedTypeSymbol aspireExportAttribute)
    {
        // Check for Task
        try
        {
            var taskType = wellKnownTypes.Get(WellKnownTypeData.WellKnownType.System_Threading_Tasks_Task);
            if (SymbolEqualityComparer.Default.Equals(type, taskType))
            {
                return true;
            }
        }
        catch (InvalidOperationException)
        {
            // Type not found
        }

        // Check for Task<T>
        if (type is INamedTypeSymbol namedType && namedType.IsGenericType)
        {
            try
            {
                var taskOfTType = wellKnownTypes.Get(WellKnownTypeData.WellKnownType.System_Threading_Tasks_Task_1);
                if (SymbolEqualityComparer.Default.Equals(namedType.OriginalDefinition, taskOfTType))
                {
                    // Validate the T in Task<T> is also ATS-compatible
                    return namedType.TypeArguments.Length == 1 &&
                           IsAtsCompatibleValueType(namedType.TypeArguments[0], wellKnownTypes, aspireExportAttribute);
                }
            }
            catch (InvalidOperationException)
            {
                // Type not found
            }
        }

        return false;
    }

    private static bool IsAtsCompatibleValueType(
        ITypeSymbol type,
        WellKnownTypes wellKnownTypes,
        INamedTypeSymbol? aspireExportAttribute = null)
    {
        // Handle nullable types
        if (type is INamedTypeSymbol namedType &&
            namedType.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T &&
            namedType.TypeArguments.Length == 1)
        {
            type = namedType.TypeArguments[0];
        }

        // Simple/primitive types (includes System.Object)
        if (IsSimpleType(type, wellKnownTypes))
        {
            return true;
        }

        // Enums
        if (type.TypeKind == TypeKind.Enum)
        {
            return true;
        }

        // Arrays of ATS-compatible types
        if (type is IArrayTypeSymbol arrayType)
        {
            return IsAtsCompatibleValueType(arrayType.ElementType, wellKnownTypes, aspireExportAttribute);
        }

        // Collection types (Dictionary, List, IReadOnlyList, etc.)
        if (IsAtsCompatibleCollectionType(type, wellKnownTypes, aspireExportAttribute))
        {
            return true;
        }

        // IResource types
        if (IsResourceType(type, wellKnownTypes))
        {
            return true;
        }

        // IResourceBuilder<T> types
        if (IsResourceBuilderType(type, wellKnownTypes))
        {
            return true;
        }

        // Types with [AspireExport] attribute
        if (aspireExportAttribute != null && HasAspireExportAttribute(type, aspireExportAttribute))
        {
            return true;
        }

        return false;
    }

    private static bool IsSimpleType(ITypeSymbol type, WellKnownTypes wellKnownTypes)
    {
        // Primitives via SpecialType
        if (type.SpecialType switch
        {
            SpecialType.System_Boolean => true,
            SpecialType.System_Byte => true,
            SpecialType.System_SByte => true,
            SpecialType.System_Int16 => true,
            SpecialType.System_UInt16 => true,
            SpecialType.System_Int32 => true,
            SpecialType.System_UInt32 => true,
            SpecialType.System_Int64 => true,
            SpecialType.System_UInt64 => true,
            SpecialType.System_Single => true,
            SpecialType.System_Double => true,
            SpecialType.System_Decimal => true,
            SpecialType.System_Char => true,
            SpecialType.System_String => true,
            SpecialType.System_DateTime => true,
            SpecialType.System_Object => true, // Maps to 'any' in ATS
            _ => false
        })
        {
            return true;
        }

        // Well-known scalar types using symbol comparison
        return IsWellKnownScalarType(type, wellKnownTypes);
    }

    private static bool IsWellKnownScalarType(ITypeSymbol type, WellKnownTypes wellKnownTypes)
    {
        // Date/time types
        if (TryMatchType(type, wellKnownTypes, WellKnownTypeData.WellKnownType.System_DateTimeOffset) ||
            TryMatchType(type, wellKnownTypes, WellKnownTypeData.WellKnownType.System_TimeSpan) ||
            TryMatchType(type, wellKnownTypes, WellKnownTypeData.WellKnownType.System_DateOnly) ||
            TryMatchType(type, wellKnownTypes, WellKnownTypeData.WellKnownType.System_TimeOnly))
        {
            return true;
        }

        // Other scalar types
        if (TryMatchType(type, wellKnownTypes, WellKnownTypeData.WellKnownType.System_Guid) ||
            TryMatchType(type, wellKnownTypes, WellKnownTypeData.WellKnownType.System_Uri))
        {
            return true;
        }

        return false;
    }

    private static bool TryMatchType(ITypeSymbol type, WellKnownTypes wellKnownTypes, WellKnownTypeData.WellKnownType wellKnownType)
    {
        try
        {
            var knownType = wellKnownTypes.Get(wellKnownType);
            return SymbolEqualityComparer.Default.Equals(type, knownType);
        }
        catch (InvalidOperationException)
        {
            // Type not found in compilation
            return false;
        }
    }

    private static bool TryMatchGenericType(ITypeSymbol type, WellKnownTypes wellKnownTypes, WellKnownTypeData.WellKnownType wellKnownType)
    {
        if (type is not INamedTypeSymbol namedType || !namedType.IsGenericType)
        {
            return false;
        }

        try
        {
            var knownType = wellKnownTypes.Get(wellKnownType);
            return SymbolEqualityComparer.Default.Equals(namedType.OriginalDefinition, knownType);
        }
        catch (InvalidOperationException)
        {
            // Type not found in compilation
            return false;
        }
    }

    private static bool IsAtsCompatibleCollectionType(
        ITypeSymbol type,
        WellKnownTypes wellKnownTypes,
        INamedTypeSymbol? aspireExportAttribute)
    {
        if (type is not INamedTypeSymbol namedType || !namedType.IsGenericType)
        {
            return false;
        }

        // Dictionary<K,V> and IDictionary<K,V>
        if (TryMatchGenericType(type, wellKnownTypes, WellKnownTypeData.WellKnownType.System_Collections_Generic_Dictionary_2) ||
            TryMatchGenericType(type, wellKnownTypes, WellKnownTypeData.WellKnownType.System_Collections_Generic_IDictionary_2))
        {
            // Validate key and value types are ATS-compatible
            return namedType.TypeArguments.Length == 2 &&
                   IsAtsCompatibleValueType(namedType.TypeArguments[0], wellKnownTypes, aspireExportAttribute) &&
                   IsAtsCompatibleValueType(namedType.TypeArguments[1], wellKnownTypes, aspireExportAttribute);
        }

        // List<T> and IList<T>
        if (TryMatchGenericType(type, wellKnownTypes, WellKnownTypeData.WellKnownType.System_Collections_Generic_List_1) ||
            TryMatchGenericType(type, wellKnownTypes, WellKnownTypeData.WellKnownType.System_Collections_Generic_IList_1))
        {
            return namedType.TypeArguments.Length == 1 &&
                   IsAtsCompatibleValueType(namedType.TypeArguments[0], wellKnownTypes, aspireExportAttribute);
        }

        // IReadOnlyList<T> and IReadOnlyCollection<T>
        if (TryMatchGenericType(type, wellKnownTypes, WellKnownTypeData.WellKnownType.System_Collections_Generic_IReadOnlyList_1) ||
            TryMatchGenericType(type, wellKnownTypes, WellKnownTypeData.WellKnownType.System_Collections_Generic_IReadOnlyCollection_1))
        {
            return namedType.TypeArguments.Length == 1 &&
                   IsAtsCompatibleValueType(namedType.TypeArguments[0], wellKnownTypes, aspireExportAttribute);
        }

        // IReadOnlyDictionary<K,V>
        if (TryMatchGenericType(type, wellKnownTypes, WellKnownTypeData.WellKnownType.System_Collections_Generic_IReadOnlyDictionary_2))
        {
            return namedType.TypeArguments.Length == 2 &&
                   IsAtsCompatibleValueType(namedType.TypeArguments[0], wellKnownTypes, aspireExportAttribute) &&
                   IsAtsCompatibleValueType(namedType.TypeArguments[1], wellKnownTypes, aspireExportAttribute);
        }

        return false;
    }

    private static bool HasAspireExportAttribute(ITypeSymbol type, INamedTypeSymbol aspireExportAttribute)
    {
        // Check direct attributes on the type
        foreach (var attr in type.GetAttributes())
        {
            if (SymbolEqualityComparer.Default.Equals(attr.AttributeClass, aspireExportAttribute))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsResourceType(ITypeSymbol type, WellKnownTypes wellKnownTypes)
    {
        try
        {
            var iResourceType = wellKnownTypes.Get(WellKnownTypeData.WellKnownType.Aspire_Hosting_ApplicationModel_IResource);
            return WellKnownTypes.Implements(type, iResourceType) ||
                   SymbolEqualityComparer.Default.Equals(type, iResourceType);
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    private static bool IsResourceBuilderType(ITypeSymbol type, WellKnownTypes wellKnownTypes)
    {
        if (type is not INamedTypeSymbol namedType)
        {
            return false;
        }

        try
        {
            var iResourceBuilderType = wellKnownTypes.Get(WellKnownTypeData.WellKnownType.Aspire_Hosting_ApplicationModel_IResourceBuilder_1);

            // Check if type itself is IResourceBuilder<T>
            if (namedType.IsGenericType &&
                SymbolEqualityComparer.Default.Equals(namedType.OriginalDefinition, iResourceBuilderType))
            {
                return true;
            }

            // Check interfaces for IResourceBuilder<T>
            foreach (var iface in namedType.AllInterfaces)
            {
                if (iface.IsGenericType &&
                    SymbolEqualityComparer.Default.Equals(iface.OriginalDefinition, iResourceBuilderType))
                {
                    return true;
                }
            }
        }
        catch (InvalidOperationException)
        {
            // Type not found
        }

        return false;
    }

    private static bool IsAtsCompatibleParameter(
        IParameterSymbol parameter,
        WellKnownTypes wellKnownTypes,
        INamedTypeSymbol aspireExportAttribute)
    {
        var type = parameter.Type;

        // Delegate types (Func<>, Action<>, custom delegates) are allowed as callbacks
        if (IsDelegateType(type))
        {
            return true;
        }

        // params arrays are allowed if element type is compatible
        if (parameter.IsParams && type is IArrayTypeSymbol arrayType)
        {
            return IsAtsCompatibleValueType(arrayType.ElementType, wellKnownTypes, aspireExportAttribute);
        }

        return IsAtsCompatibleValueType(type, wellKnownTypes, aspireExportAttribute);
    }

    private static bool IsDelegateType(ITypeSymbol type)
    {
        if (type is INamedTypeSymbol namedType)
        {
            return namedType.TypeKind == TypeKind.Delegate;
        }
        return false;
    }
}
