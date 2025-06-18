// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;
using System.Text.Json.Nodes;

namespace Aspire.Cli;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(CliSettings))]
[JsonSerializable(typeof(JsonObject))]
internal partial class JsonSourceGenerationContext : JsonSerializerContext
{
}
