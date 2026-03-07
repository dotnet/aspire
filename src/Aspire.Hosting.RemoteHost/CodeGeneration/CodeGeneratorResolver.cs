// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.RemoteHost.CodeGeneration;

/// <summary>
/// Resolves code generators by language, discovering them from loaded assemblies.
/// </summary>
internal sealed class CodeGeneratorResolver
{
    private readonly Lazy<Dictionary<string, IsolatedCodeGenerator>> _generators;
    private readonly ILogger<CodeGeneratorResolver> _logger;

    public CodeGeneratorResolver(
        IServiceProvider serviceProvider,
        AssemblyLoader assemblyLoader,
        ILogger<CodeGeneratorResolver> logger)
    {
        _logger = logger;
        var generatorInterface = assemblyLoader.GetRequiredAssembly("Aspire.Hosting").GetType("Aspire.Hosting.Ats.ICodeGenerator", throwOnError: true)
            ?? throw new InvalidOperationException("Aspire.Hosting.Ats.ICodeGenerator was not found.");
        _generators = new Lazy<Dictionary<string, IsolatedCodeGenerator>>(
            () => DiscoverGenerators(serviceProvider, assemblyLoader.GetAssemblies(), generatorInterface));
    }

    /// <summary>
    /// Gets a code generator for the specified language.
    /// </summary>
    /// <param name="language">The target language (e.g., "TypeScript", "Python").</param>
    /// <returns>The code generator, or null if not found.</returns>
    public IsolatedCodeGenerator? GetCodeGenerator(string language)
    {
        _generators.Value.TryGetValue(language, out var generator);
        return generator;
    }

    private Dictionary<string, IsolatedCodeGenerator> DiscoverGenerators(
        IServiceProvider serviceProvider,
        IReadOnlyList<Assembly> assemblies,
        Type generatorInterface)
    {
        return IsolatedImplementationDiscovery.DiscoverByLanguage(
            serviceProvider,
            assemblies,
            generatorInterface,
            _logger,
            implementationDescription: "code generator",
            createAdapter: static (instance, type) => new IsolatedCodeGenerator(instance, type),
            getLanguage: static generator => generator.Language);
    }
}

internal sealed class IsolatedCodeGenerator
{
    private readonly object _instance;
    private readonly MethodInfo _generateDistributedApplicationMethod;

    public IsolatedCodeGenerator(object instance, Type implementationType)
    {
        _instance = instance;
        Language = implementationType.GetProperty("Language")?.GetValue(instance) as string
            ?? throw new InvalidOperationException($"Code generator '{implementationType.FullName}' did not expose a Language property.");
        _generateDistributedApplicationMethod = implementationType.GetMethod("GenerateDistributedApplication", BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException($"Code generator '{implementationType.FullName}' did not expose GenerateDistributedApplication.");
    }

    public string Language { get; }

    public Dictionary<string, string> GenerateDistributedApplication(object isolatedContext)
    {
        try
        {
            return (Dictionary<string, string>)(_generateDistributedApplicationMethod.Invoke(_instance, [isolatedContext])
                ?? throw new InvalidOperationException($"Code generator '{_instance.GetType().FullName}' returned null."));
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            throw ex.InnerException;
        }
    }
}
