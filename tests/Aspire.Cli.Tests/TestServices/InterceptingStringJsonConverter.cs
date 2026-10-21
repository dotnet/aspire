// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Aspire.Cli.Tests.TestServices;

internal sealed class InterceptingStringJsonConverter(Action onWrite) : JsonConverter<string>
{
    public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => throw new NotSupportedException();

    public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
    {
        onWrite();
        writer.WriteStringValue(value);
    }
}
