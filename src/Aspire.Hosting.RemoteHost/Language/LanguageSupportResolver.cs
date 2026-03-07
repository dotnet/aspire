// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Aspire.Hosting.RemoteHost.Ats;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.RemoteHost.Language;

/// <summary>
/// Resolves language support implementations by language, discovering them from loaded assemblies.
/// </summary>
internal sealed class LanguageSupportResolver
{
    private readonly Lazy<Dictionary<string, IsolatedLanguageSupport>> _languages;
    private readonly ILogger<LanguageSupportResolver> _logger;

    public LanguageSupportResolver(
        IServiceProvider serviceProvider,
        AssemblyLoader assemblyLoader,
        ILogger<LanguageSupportResolver> logger)
    {
        _logger = logger;
        var hostingAssembly = assemblyLoader.GetRequiredAssembly("Aspire.Hosting");
        var languageInterface = hostingAssembly.GetType("Aspire.Hosting.Ats.ILanguageSupport", throwOnError: true)
            ?? throw new InvalidOperationException("Aspire.Hosting.Ats.ILanguageSupport was not found.");
        var scaffoldRequestType = hostingAssembly.GetType("Aspire.Hosting.Ats.ScaffoldRequest", throwOnError: true)
            ?? throw new InvalidOperationException("Aspire.Hosting.Ats.ScaffoldRequest was not found.");

        _languages = new Lazy<Dictionary<string, IsolatedLanguageSupport>>(
            () => DiscoverLanguages(serviceProvider, assemblyLoader.GetAssemblies(), languageInterface, scaffoldRequestType));
    }

    /// <summary>
    /// Gets language support for the specified language.
    /// </summary>
    /// <param name="language">The target language (e.g., "TypeScript", "Python").</param>
    /// <returns>The language support, or null if not found.</returns>
    public IsolatedLanguageSupport? GetLanguageSupport(string language)
    {
        _languages.Value.TryGetValue(language, out var support);
        return support;
    }

    /// <summary>
    /// Gets all available language support implementations.
    /// </summary>
    /// <returns>All discovered language support implementations.</returns>
    public IEnumerable<IsolatedLanguageSupport> GetAllLanguages()
    {
        return _languages.Value.Values;
    }

    private Dictionary<string, IsolatedLanguageSupport> DiscoverLanguages(
        IServiceProvider serviceProvider,
        IReadOnlyList<Assembly> assemblies,
        Type languageInterface,
        Type scaffoldRequestType)
    {
        return IsolatedImplementationDiscovery.DiscoverByLanguage(
            serviceProvider,
            assemblies,
            languageInterface,
            _logger,
            implementationDescription: "language support",
            createAdapter: (instance, type) => new IsolatedLanguageSupport(instance, type, scaffoldRequestType),
            getLanguage: static language => language.Language);
    }
}

internal sealed class IsolatedLanguageSupport
{
    private readonly object _instance;
    private readonly Type _scaffoldRequestType;
    private readonly MethodInfo _scaffoldMethod;
    private readonly MethodInfo _detectMethod;
    private readonly MethodInfo _getRuntimeSpecMethod;

    public IsolatedLanguageSupport(object instance, Type implementationType, Type scaffoldRequestType)
    {
        _instance = instance;
        _scaffoldRequestType = scaffoldRequestType;
        Language = implementationType.GetProperty("Language")?.GetValue(instance) as string
            ?? throw new InvalidOperationException($"Language support '{implementationType.FullName}' did not expose a Language property.");
        _scaffoldMethod = implementationType.GetMethod("Scaffold", BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException($"Language support '{implementationType.FullName}' did not expose Scaffold.");
        _detectMethod = implementationType.GetMethod("Detect", BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException($"Language support '{implementationType.FullName}' did not expose Detect.");
        _getRuntimeSpecMethod = implementationType.GetMethod("GetRuntimeSpec", BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException($"Language support '{implementationType.FullName}' did not expose GetRuntimeSpec.");
    }

    public string Language { get; }

    public Dictionary<string, string> Scaffold(ScaffoldRequest request)
    {
        var isolatedRequest = Activator.CreateInstance(_scaffoldRequestType)
            ?? throw new InvalidOperationException("Failed to create an isolated ScaffoldRequest instance.");

        SetProperty(isolatedRequest, "TargetPath", request.TargetPath);
        SetProperty(isolatedRequest, "ProjectName", request.ProjectName);
        SetProperty(isolatedRequest, "PortSeed", request.PortSeed);

        return Invoke<Dictionary<string, string>>(_scaffoldMethod, isolatedRequest);
    }

    public DetectionResult Detect(string directoryPath)
    {
        var result = Invoke<object>(_detectMethod, directoryPath);

        return new DetectionResult
        {
            IsValid = GetRequiredValue<bool>(result, "IsValid"),
            Language = GetOptionalValue<string>(result, nameof(DetectionResult.Language)),
            AppHostFile = GetOptionalValue<string>(result, nameof(DetectionResult.AppHostFile))
        };
    }

    public RuntimeSpec GetRuntimeSpec()
    {
        var spec = Invoke<object>(_getRuntimeSpecMethod);

        return new RuntimeSpec
        {
            Language = GetRequiredValue<string>(spec, nameof(RuntimeSpec.Language)),
            DisplayName = GetRequiredValue<string>(spec, nameof(RuntimeSpec.DisplayName)),
            CodeGenLanguage = GetRequiredValue<string>(spec, nameof(RuntimeSpec.CodeGenLanguage)),
            DetectionPatterns = GetRequiredValue<string[]>(spec, nameof(RuntimeSpec.DetectionPatterns)),
            InstallDependencies = GetOptionalValue<object>(spec, nameof(RuntimeSpec.InstallDependencies)) is { } install ? MapCommandSpec(install) : null,
            Execute = MapCommandSpec(GetRequiredValue<object>(spec, nameof(RuntimeSpec.Execute))),
            WatchExecute = GetOptionalValue<object>(spec, nameof(RuntimeSpec.WatchExecute)) is { } watch ? MapCommandSpec(watch) : null,
            PublishExecute = GetOptionalValue<object>(spec, nameof(RuntimeSpec.PublishExecute)) is { } publish ? MapCommandSpec(publish) : null
        };
    }

    private T Invoke<T>(MethodInfo method, params object?[] arguments)
    {
        try
        {
            return (T)(method.Invoke(_instance, arguments)
                ?? throw new InvalidOperationException($"Invocation of '{method.Name}' returned null."));
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            throw ex.InnerException;
        }
    }

    private static CommandSpec MapCommandSpec(object source) => new()
    {
        Command = GetRequiredValue<string>(source, "Command"),
        Args = GetRequiredValue<string[]>(source, "Args"),
        EnvironmentVariables = GetOptionalValue<Dictionary<string, string>>(source, "EnvironmentVariables")
    };

    private static void SetProperty(object target, string propertyName, object? value)
    {
        var property = target.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException($"Property '{propertyName}' was not found on '{target.GetType().FullName}'.");
        property.SetValue(target, value);
    }

    private static T GetRequiredValue<T>(object source, string propertyName) =>
        GetOptionalValue<T>(source, propertyName)
        ?? throw new InvalidOperationException($"Property '{propertyName}' on '{source.GetType().FullName}' was null.");

    private static T? GetOptionalValue<T>(object source, string propertyName)
    {
        var property = source.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException($"Property '{propertyName}' was not found on '{source.GetType().FullName}'.");
        return (T?)property.GetValue(source);
    }
}
