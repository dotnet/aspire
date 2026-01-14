// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Ats;
using Microsoft.Extensions.Logging;
using StreamJsonRpc;

namespace Aspire.Hosting.RemoteHost.CodeGeneration;

/// <summary>
/// JSON-RPC service for generating language-specific SDK code.
/// </summary>
internal sealed class CodeGenerationService
{
    private readonly AtsContextFactory _atsContextFactory;
    private readonly CodeGeneratorResolver _resolver;
    private readonly ILogger<CodeGenerationService> _logger;

    public CodeGenerationService(
        AtsContextFactory atsContextFactory,
        CodeGeneratorResolver resolver,
        ILogger<CodeGenerationService> logger)
    {
        _atsContextFactory = atsContextFactory;
        _resolver = resolver;
        _logger = logger;
    }

    /// <summary>
    /// Generates SDK code for the specified language.
    /// </summary>
    /// <param name="language">The target language (e.g., "TypeScript", "Python").</param>
    /// <returns>A dictionary of file paths to file contents.</returns>
    [JsonRpcMethod("generateCode")]
    public Dictionary<string, string> GenerateCode(string language)
    {
        _logger.LogDebug(">> generateCode({Language})", language);
        var sw = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            var generator = _resolver.GetCodeGenerator(language);
            if (generator == null)
            {
                throw new ArgumentException($"No code generator found for language: {language}");
            }

            var files = generator.GenerateDistributedApplication(_atsContextFactory.GetContext());

            _logger.LogDebug("<< generateCode({Language}) completed in {ElapsedMs}ms, generated {FileCount} files", language, sw.ElapsedMilliseconds, files.Count);
            return files;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "<< generateCode({Language}) failed", language);
            throw;
        }
    }
}
