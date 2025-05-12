// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Aspire.Hosting.Analyzers;

public partial class AppHostAnalyzer
{
    internal static class Diagnostics
    {
        private const string ModelNameMustBeValidId = "ASPIRE006";
        internal static readonly DiagnosticDescriptor s_modelNameMustBeValid = new(
            id: ModelNameMustBeValidId,
            title: "Application model items must have valid names",
            messageFormat: "{0}",
            category: "Design",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            helpLinkUri: $"https://aka.ms/aspire/diagnostics/{ModelNameMustBeValidId}");

        public static readonly ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics = ImmutableArray.Create(
            s_modelNameMustBeValid
        );
    }
}
