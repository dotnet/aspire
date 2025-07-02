// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using Spectre.Console;
using StreamJsonRpc;
using StreamJsonRpc.Reflection;

namespace Aspire.Cli.Backchannel;

[JsonSerializable(typeof(string[]))]
[JsonSerializable(typeof(bool))]
[JsonSerializable(typeof(long))]
[JsonSerializable(typeof(DashboardUrlsState))]
[JsonSerializable(typeof(JsonElement))]
[JsonSerializable(typeof(IAsyncEnumerable<RpcResourceState>))]
[JsonSerializable(typeof(MessageFormatterEnumerableTracker.EnumeratorResults<RpcResourceState>))]
[JsonSerializable(typeof(IAsyncEnumerable<BackchannelLogEntry>))]
[JsonSerializable(typeof(MessageFormatterEnumerableTracker.EnumeratorResults<BackchannelLogEntry>))]
[JsonSerializable(typeof(IAsyncEnumerable<PublishingActivity>))]
[JsonSerializable(typeof(MessageFormatterEnumerableTracker.EnumeratorResults<PublishingActivity>))]
[JsonSerializable(typeof(RequestId))]
[JsonSerializable(typeof(IEnumerable<DisplayLineState>))]
[JsonSerializable(typeof(ValidationResult))]
[JsonSerializable(typeof(IAsyncEnumerable<CommandOutput>))]
[JsonSerializable(typeof(MessageFormatterEnumerableTracker.EnumeratorResults<CommandOutput>))]
internal partial class BackchannelJsonSerializerContext : JsonSerializerContext
{
    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "Using the Json source generator.")]
    [UnconditionalSuppressMessage("AotAnalysis", "IL3050:RequiresDynamicCode", Justification = "Using the Json source generator.")]
    internal static SystemTextJsonFormatter CreateRpcMessageFormatter()
    {
        var formatter = new SystemTextJsonFormatter();
        formatter.JsonSerializerOptions.TypeInfoResolver = Default;
        return formatter;
    }
}
