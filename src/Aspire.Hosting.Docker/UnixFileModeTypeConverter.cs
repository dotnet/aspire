// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Aspire.Hosting.Docker;

internal class UnixFileModeTypeConverter : IYamlTypeConverter
{
    public bool Accepts(Type type)
    {
        return type == typeof(UnixFileMode);
    }

    public object? ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
    {
        if (parser.Current is not YamlDotNet.Core.Events.Scalar scalar)
        {
            throw new InvalidOperationException(parser.Current?.ToString());
        }

        var value = scalar.Value;
        parser.MoveNext();

        return Convert.ToInt32(value, 8);
    }

    public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer serializer)
    {
        if (value is not UnixFileMode mode)
        {
            throw new InvalidOperationException($"Expected {nameof(UnixFileMode)} but got {value?.GetType()}");
        }

        emitter.Emit(new Scalar("0" + Convert.ToString((int)mode, 8)));
    }
}