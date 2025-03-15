// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Aspire.Hosting.Kubernetes.Yaml;

internal sealed class ByteArrayStringYamlConverter : IYamlTypeConverter
{
    public bool Accepts(Type type)
    {
        return type == typeof(byte[]);
    }

    public object? ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
    {
        if (parser.Current is Scalar scalar)
        {
            try
            {
                return string.IsNullOrEmpty(scalar.Value) ? null : Encoding.UTF8.GetBytes(scalar.Value);
            }
            finally
            {
                parser.MoveNext();
            }
        }

        throw new InvalidOperationException(parser.Current?.ToString());
    }

    public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer serializer)
    {
        if (value == null)
        {
            return;
        }

        var obj = (byte[])value;
        emitter.Emit(new Scalar(Encoding.UTF8.GetString(obj)));
    }
}
