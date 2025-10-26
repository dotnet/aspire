// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Aspire.Hosting.Docker;

/// <summary>
/// Custom YAML type converter for environment variables that handles both array and dictionary formats.
/// </summary>
internal class EnvironmentVariablesTypeConverter : IYamlTypeConverter
{
    public bool Accepts(Type type)
    {
        return type == typeof(Dictionary<string, string>);
    }

    public object? ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);

        if (parser.TryConsume<MappingStart>(out _))
        {
            // Dictionary format: {KEY: value, KEY2: value2}
            while (!parser.TryConsume<MappingEnd>(out _))
            {
                var key = parser.Consume<Scalar>().Value;
                var value = parser.Consume<Scalar>().Value;
                result[key] = value;
            }
        }
        else if (parser.TryConsume<SequenceStart>(out _))
        {
            // Array format: ["KEY=value", "KEY2=value2"]
            while (!parser.TryConsume<SequenceEnd>(out _))
            {
                var item = parser.Consume<Scalar>().Value;
                var parts = item.Split('=', 2);
                if (parts.Length == 2)
                {
                    result[parts[0]] = parts[1];
                }
                else if (parts.Length == 1)
                {
                    // Environment variable without value
                    result[parts[0]] = string.Empty;
                }
            }
        }

        return result;
    }

    public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer serializer)
    {
        if (value is not Dictionary<string, string> dict)
        {
            emitter.Emit(new MappingStart());
            emitter.Emit(new MappingEnd());
            return;
        }

        emitter.Emit(new MappingStart());
        foreach (var (key, val) in dict)
        {
            emitter.Emit(new Scalar(key));
            emitter.Emit(new Scalar(val));
        }
        emitter.Emit(new MappingEnd());
    }
}
