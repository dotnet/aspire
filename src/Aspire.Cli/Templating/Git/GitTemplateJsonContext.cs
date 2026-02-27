// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Aspire.Cli.Templating.Git;

[JsonSourceGenerationOptions(
    WriteIndented = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(GitTemplateManifest))]
[JsonSerializable(typeof(GitTemplateIndex))]
internal sealed partial class GitTemplateJsonContext : JsonSerializerContext
{
    private static GitTemplateJsonContext? s_relaxedEscaping;

    /// <summary>
    /// Gets a context configured with relaxed JSON escaping for user-facing output.
    /// </summary>
    public static GitTemplateJsonContext RelaxedEscaping => s_relaxedEscaping ??= new(new JsonSerializerOptions
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    });
}
