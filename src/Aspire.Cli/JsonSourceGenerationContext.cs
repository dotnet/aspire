// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;
using System.Text.Json.Nodes;
using Aspire.Cli.Mcp;
using Aspire.Cli.Utils;

namespace Aspire.Cli;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(CliSettings))]
[JsonSerializable(typeof(JsonObject))]
[JsonSerializable(typeof(ListIntegrationsResponse))]
[JsonSerializable(typeof(Integration))]
[JsonSerializable(typeof(DoctorCheckResponse))]
[JsonSerializable(typeof(EnvironmentCheckResult))]
[JsonSerializable(typeof(DoctorCheckSummary))]
internal partial class JsonSourceGenerationContext : JsonSerializerContext
{
}
