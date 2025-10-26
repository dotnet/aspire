// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Aspire.Hosting.Docker;

/// <summary>
/// Custom YAML type converter for port mappings that handles both short and long syntax formats.
/// </summary>
internal class PortMappingsTypeConverter : IYamlTypeConverter
{
    public bool Accepts(Type type)
    {
        // Only accept List<string> types
        return type.IsGenericType && 
               type.GetGenericTypeDefinition() == typeof(List<>) && 
               type.GetGenericArguments()[0] == typeof(string);
    }

    public object? ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
    {
        var result = new List<string>();

        if (!parser.TryConsume<SequenceStart>(out _))
        {
            return result;
        }

        while (!parser.TryConsume<SequenceEnd>(out _))
        {
            if (parser.Current is Scalar scalar)
            {
                // Short syntax: "8080:80" or "8080:80/tcp"
                result.Add(scalar.Value);
                parser.MoveNext();
            }
            else if (parser.Current is MappingStart)
            {
                // Long syntax: {target: 80, published: 8080, protocol: tcp, host_ip: "127.0.0.1"}
                // Convert to short syntax string
                parser.MoveNext(); // consume MappingStart

                int? target = null;
                int? published = null;
                string? protocol = null;
                string? hostIp = null;

                while (parser.Current is not MappingEnd)
                {
                    if (parser.Current is Scalar key)
                    {
                        var keyValue = key.Value;
                        parser.MoveNext();

                        if (parser.Current is Scalar value)
                        {
                            switch (keyValue)
                            {
                                case "target":
                                    target = int.Parse(value.Value, System.Globalization.CultureInfo.InvariantCulture);
                                    break;
                                case "published":
                                    published = int.Parse(value.Value, System.Globalization.CultureInfo.InvariantCulture);
                                    break;
                                case "protocol":
                                    protocol = value.Value;
                                    break;
                                case "host_ip":
                                    hostIp = value.Value;
                                    break;
                            }
                            parser.MoveNext();
                        }
                    }
                    else
                    {
                        parser.MoveNext();
                    }
                }

                parser.MoveNext(); // consume MappingEnd

                // Convert to short syntax string
                var portString = "";
                if (!string.IsNullOrEmpty(hostIp))
                {
                    portString = $"{hostIp}:";
                }

                if (published.HasValue && target.HasValue)
                {
                    portString += $"{published}:{target}";
                }
                else if (target.HasValue)
                {
                    portString += $"{target}";
                }

                if (!string.IsNullOrEmpty(protocol))
                {
                    portString += $"/{protocol}";
                }

                if (!string.IsNullOrEmpty(portString))
                {
                    result.Add(portString);
                }
            }
            else
            {
                parser.MoveNext();
            }
        }

        return result;
    }

    public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer serializer)
    {
        if (value is not List<string> list)
        {
            emitter.Emit(new SequenceStart(null, null, true, SequenceStyle.Block));
            emitter.Emit(new SequenceEnd());
            return;
        }

        emitter.Emit(new SequenceStart(null, null, true, SequenceStyle.Block));
        foreach (var item in list)
        {
            emitter.Emit(new Scalar(item));
        }
        emitter.Emit(new SequenceEnd());
    }
}
