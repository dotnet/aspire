// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Nodes;
using Aspire.Cli.Certificates;
using Aspire.Cli.Configuration;
using Aspire.Cli.Mcp;
using Aspire.Cli.Utils.EnvironmentChecker;

namespace Aspire.Cli;

[JsonSourceGenerationOptions(WriteIndented = true, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(CliSettings))]
[JsonSerializable(typeof(JsonObject))]
[JsonSerializable(typeof(ListIntegrationsResponse))]
[JsonSerializable(typeof(Integration))]
[JsonSerializable(typeof(DoctorCheckResponse))]
[JsonSerializable(typeof(EnvironmentCheckResult))]
[JsonSerializable(typeof(DoctorCheckSummary))]
[JsonSerializable(typeof(ContainerVersionJson))]
[JsonSerializable(typeof(AspireJsonConfiguration))]
[JsonSerializable(typeof(List<DevCertInfo>))]
internal partial class JsonSourceGenerationContext : JsonSerializerContext
{
    private static JsonSourceGenerationContext? s_relaxedEscaping;

    /// <summary>
    /// Gets a context configured with relaxed JSON escaping that preserves non-ASCII characters
    /// (e.g., Chinese, Japanese, Korean) instead of escaping them to \uXXXX sequences.
    /// Use this for JSON output that will be displayed to users.
    /// </summary>
    public static JsonSourceGenerationContext RelaxedEscaping => s_relaxedEscaping ??= new(new JsonSerializerOptions
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    });
}
