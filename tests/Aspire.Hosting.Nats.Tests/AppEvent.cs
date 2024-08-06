// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace Aspire.Hosting.Nats.Tests;

public record AppEvent(string Subject, string Name, string Description, decimal Priority);

[JsonSerializable(typeof(AppEvent))]
[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
public partial class AppJsonContext : JsonSerializerContext
{
}
