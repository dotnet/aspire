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
        internal static readonly DiagnosticDescriptor s_resourceMustHaveValidName = new(
            "ASR0000",
            "Ensure resources have valid names",
            "Resource names: must start with an ASCII letter; must contain only ASCII letters, digits, and hyphens; must not end with a hyphen; nust not contain consecutive hyphens; and must be between 1 and 64 characters long",
            "Design",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            helpLinkUri: "https://aka.ms/dotnet/aspire/asr0000");

        public static readonly ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics = [
            // Resources
            s_resourceMustHaveValidName,
        ];
    }
}
