// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Aspire.Hosting.Analyzers;

public partial class AspireExportAnalyzer
{
    internal static class Diagnostics
    {
        private const string ExportMethodMustBeStaticId = "ASPIRE007";
        internal static readonly DiagnosticDescriptor s_exportMethodMustBeStatic = new(
            id: ExportMethodMustBeStaticId,
            title: "AspireExport method must be static",
            messageFormat: "Method '{0}' marked with [AspireExport] must be static",
            category: "Design",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            helpLinkUri: $"https://aka.ms/aspire/diagnostics/{ExportMethodMustBeStaticId}");

        private const string InvalidExportIdFormatId = "ASPIRE008";
        internal static readonly DiagnosticDescriptor s_invalidExportIdFormat = new(
            id: InvalidExportIdFormatId,
            title: "Invalid AspireExport ID format",
            messageFormat: "Export ID '{0}' is not a valid method name. Use a valid identifier (e.g., 'addRedis', 'withEnvironment').",
            category: "Design",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            helpLinkUri: $"https://aka.ms/aspire/diagnostics/{InvalidExportIdFormatId}");

        private const string ReturnTypeMustBeAtsCompatibleId = "ASPIRE009";
        internal static readonly DiagnosticDescriptor s_returnTypeMustBeAtsCompatible = new(
            id: ReturnTypeMustBeAtsCompatibleId,
            title: "AspireExport return type must be ATS-compatible",
            messageFormat: "Method '{0}' has return type '{1}' which is not ATS-compatible. Use void, Task, Task<T>, or a supported Aspire type.",
            category: "Design",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            helpLinkUri: $"https://aka.ms/aspire/diagnostics/{ReturnTypeMustBeAtsCompatibleId}");

        private const string ParameterTypeMustBeAtsCompatibleId = "ASPIRE010";
        internal static readonly DiagnosticDescriptor s_parameterTypeMustBeAtsCompatible = new(
            id: ParameterTypeMustBeAtsCompatibleId,
            title: "AspireExport parameter type must be ATS-compatible",
            messageFormat: "Parameter '{0}' of type '{1}' in method '{2}' is not ATS-compatible. Use primitive types, enums, or supported Aspire types.",
            category: "Design",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            helpLinkUri: $"https://aka.ms/aspire/diagnostics/{ParameterTypeMustBeAtsCompatibleId}");

        public static readonly ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics = ImmutableArray.Create(
            s_exportMethodMustBeStatic,
            s_invalidExportIdFormat,
            s_returnTypeMustBeAtsCompatible,
            s_parameterTypeMustBeAtsCompatible
        );
    }
}
