// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Aspire.Hosting.Analyzers;

public partial class AppHostAnalyzer
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("MicrosoftCodeAnalysisReleaseTracking", "RS2008:Enable analyzer release tracking")]
    internal static class Diagnostics
    {
        internal static readonly DiagnosticDescriptor s_modelNameMustBeValid = new(
            id: "ASPIRE006",
            title: "Application model items must have valid names",
            messageFormat: "{0}",
            category: "Design",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            helpLinkUri: "https://aka.ms/dotnet/aspire/ASPIRE006");

        public static readonly ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics = [
            // Resources
            s_modelNameMustBeValid
        ];
    }
}
