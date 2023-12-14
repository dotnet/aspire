// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.DotnetRuntime.Extensions;

namespace Microsoft.Extensions.Configuration.Binder.SourceGeneration;

/// <summary>
/// Mocks out the Configuration.Binder.SourceGeneration project's DiagnosticDescriptors class.
/// 
/// The real diagnostic descriptors can't be used because they use localized strings, which
/// would require getting dotnet/runtime's "SR" generator working in this repo.
/// </summary>
internal static class DiagnosticDescriptors
{
    private static readonly string s_projectName = typeof(DiagnosticDescriptors).Assembly.GetName().Name!;

    public static DiagnosticDescriptor TypeNotSupported { get; } = CreateTypeNotSupportedDescriptor("TypeNotSupported");
    public static DiagnosticDescriptor MissingPublicInstanceConstructor { get; } = CreateTypeNotSupportedDescriptor("MissingPublicInstanceConstructor");
    public static DiagnosticDescriptor CollectionNotSupported { get; } = CreateTypeNotSupportedDescriptor("CollectionNotSupported");
    public static DiagnosticDescriptor DictionaryKeyNotSupported { get; } = CreateTypeNotSupportedDescriptor("DictionaryKeyNotSupported");
    public static DiagnosticDescriptor ElementTypeNotSupported { get; } = CreateTypeNotSupportedDescriptor("ElementTypeNotSupported");
    public static DiagnosticDescriptor MultipleParameterizedConstructors { get; } = CreateTypeNotSupportedDescriptor("MultipleParameterizedConstructors");
    public static DiagnosticDescriptor MultiDimArraysNotSupported { get; } = CreateTypeNotSupportedDescriptor("MultiDimArraysNotSupported");
    public static DiagnosticDescriptor NullableUnderlyingTypeNotSupported { get; } = CreateTypeNotSupportedDescriptor("NullableUnderlyingTypeNotSupported");

    public static DiagnosticDescriptor PropertyNotSupported { get; } = new DiagnosticDescriptor(
        id: "SYSLIB1101",
        title: "PropertyNotSupportedTitle",
        messageFormat: "PropertyNotSupportedMessageFormat",
        category: s_projectName,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor LanguageVersionNotSupported { get; } = DiagnosticDescriptorHelper.Create(
        id: "SYSLIB1102",
        title: "LanguageVersionIsNotSupportedTitle",
        messageFormat: "LanguageVersionIsNotSupportedMessageFormat",
        category: s_projectName,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor ValueTypesInvalidForBind { get; } = DiagnosticDescriptorHelper.Create(
        id: "SYSLIB1103",
        title: "ValueTypesInvalidForBindTitle",
        messageFormat: "ValueTypesInvalidForBindMessageFormat",
        category: s_projectName,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor CouldNotDetermineTypeInfo { get; } = DiagnosticDescriptorHelper.Create(
        id: "SYSLIB1104",
        title: "CouldNotDetermineTypeInfoTitle",
        messageFormat: "CouldNotDetermineTypeInfoMessageFormat",
        category: s_projectName,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static DiagnosticDescriptor CreateTypeNotSupportedDescriptor(string nameofLocalizableMessageFormat) =>
        DiagnosticDescriptorHelper.Create(
        id: "SYSLIB1100",
        title: "TypeNotSupportedTitle",
        messageFormat: nameofLocalizableMessageFormat,
        category: s_projectName,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor GetNotSupportedDescriptor(NotSupportedReason reason) =>
        reason switch
        {
            NotSupportedReason.UnknownType => TypeNotSupported,
            NotSupportedReason.MissingPublicInstanceConstructor => MissingPublicInstanceConstructor,
            NotSupportedReason.CollectionNotSupported => CollectionNotSupported,
            NotSupportedReason.DictionaryKeyNotSupported => DictionaryKeyNotSupported,
            NotSupportedReason.ElementTypeNotSupported => ElementTypeNotSupported,
            NotSupportedReason.MultipleParameterizedConstructors => MultipleParameterizedConstructors,
            NotSupportedReason.MultiDimArraysNotSupported => MultiDimArraysNotSupported,
            NotSupportedReason.NullableUnderlyingTypeNotSupported => NullableUnderlyingTypeNotSupported,
            _ => throw new InvalidOperationException()
        };
}
