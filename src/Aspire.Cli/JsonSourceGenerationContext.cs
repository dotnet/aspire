// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;
using System.Text.Json.Nodes;
using Aspire.Cli.Certificates;
using Aspire.Cli.Commands;
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
[JsonSerializable(typeof(ConfigInfo))]
[JsonSerializable(typeof(FeatureInfo))]
[JsonSerializable(typeof(SettingsSchema))]
[JsonSerializable(typeof(PropertyInfo))]
internal partial class JsonSourceGenerationContext : JsonSerializerContext
{
}
