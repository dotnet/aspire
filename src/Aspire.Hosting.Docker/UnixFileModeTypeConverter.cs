// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Docker.Resources.ServiceNodes;
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

/// <summary>
/// Represents environment variables that can be specified in either array or dictionary format.
/// </summary>
public class EnvironmentVariables : Dictionary<string, string>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EnvironmentVariables"/> class.
    /// </summary>
    public EnvironmentVariables() : base() { }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="EnvironmentVariables"/> class from an existing dictionary.
    /// </summary>
    /// <param name="dictionary">The dictionary to copy from.</param>
    public EnvironmentVariables(IDictionary<string, string> dictionary) : base(dictionary) { }
}

/// <summary>
/// Represents a list of port mappings for a Docker Compose service.
/// Supports both short syntax (e.g., "8080:80", "127.0.0.1:8080:80/tcp") 
/// and long syntax (objects with target, published, protocol, host_ip properties).
/// </summary>
public class PortsList : List<string>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PortsList"/> class.
    /// </summary>
    public PortsList() : base() { }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="PortsList"/> class from an existing list.
    /// </summary>
    /// <param name="collection">The collection to copy from.</param>
    public PortsList(IEnumerable<string> collection) : base(collection) { }
}

/// <summary>
/// Converts Docker Compose environment variables from both array and dictionary formats to a dictionary.
/// Supports both "KEY=value" array format and "KEY: value" dictionary format.
/// </summary>
internal class EnvironmentVariablesTypeConverter : IYamlTypeConverter
{
    public bool Accepts(Type type)
    {
        return type == typeof(EnvironmentVariables);
    }

    public object? ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
    {
        var result = new EnvironmentVariables();

        if (parser.Current is SequenceStart)
        {
            // Array format: ["KEY=value", "KEY2=value2"]
            parser.MoveNext();
            while (parser.Current is not SequenceEnd)
            {
                if (parser.Current is Scalar scalar)
                {
                    var parts = scalar.Value.Split('=', 2);
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
                parser.MoveNext();
            }
            parser.MoveNext(); // Skip SequenceEnd
        }
        else if (parser.Current is MappingStart)
        {
            // Dictionary format: {KEY: value, KEY2: value2}
            parser.MoveNext();
            while (parser.Current is not MappingEnd)
            {
                if (parser.Current is Scalar keyScalar)
                {
                    var key = keyScalar.Value;
                    parser.MoveNext();
                    
                    if (parser.Current is Scalar valueScalar)
                    {
                        result[key] = valueScalar.Value;
                    }
                    parser.MoveNext();
                }
                else
                {
                    parser.MoveNext();
                }
            }
            parser.MoveNext(); // Skip MappingEnd
        }

        return result;
    }

    public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer serializer)
    {
        if (value is EnvironmentVariables dict)
        {
            serializer(dict);
        }
    }
}

/// <summary>
/// Converts Docker Compose ports from both short and long formats.
/// Supports both short syntax (e.g., "3000", "3000:3000", "127.0.0.1:3000:3000/tcp") 
/// and long syntax (target, published, protocol, host_ip properties).
/// </summary>
internal class PortsListTypeConverter : IYamlTypeConverter
{
    public bool Accepts(Type type)
    {
        return type == typeof(PortsList);
    }

    public object? ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
    {
        var result = new PortsList();

        if (parser.Current is not SequenceStart)
        {
            return result;
        }

        parser.MoveNext();
        while (parser.Current is not SequenceEnd)
        {
            if (parser.Current is Scalar scalar)
            {
                // Short syntax: "3000", "3000:3000", "127.0.0.1:3000:3000/tcp"
                result.Add(scalar.Value);
                parser.MoveNext();
            }
            else if (parser.Current is MappingStart)
            {
                // Long syntax: {target: 3000, published: 8080, protocol: tcp, host_ip: 127.0.0.1}
                parser.MoveNext(); // Move past MappingStart

                int? target = null;
                int? published = null;
                string? protocol = null;
                string? hostIp = null;

                while (parser.Current is not MappingEnd)
                {
                    if (parser.Current is Scalar keyScalar)
                    {
                        var key = keyScalar.Value;
                        parser.MoveNext();

                        if (parser.Current is Scalar valueScalar)
                        {
                            switch (key)
                            {
                                case "target":
                                    if (int.TryParse(valueScalar.Value, out var t))
                                    {
                                        target = t;
                                    }
                                    break;
                                case "published":
                                    if (int.TryParse(valueScalar.Value, out var p))
                                    {
                                        published = p;
                                    }
                                    break;
                                case "protocol":
                                    protocol = valueScalar.Value;
                                    break;
                                case "host_ip":
                                    hostIp = valueScalar.Value;
                                    break;
                                    // mode is ignored (host vs ingress)
                            }
                        }
                        parser.MoveNext();
                    }
                    else
                    {
                        parser.MoveNext();
                    }
                }

                parser.MoveNext(); // Move past MappingEnd

                // Convert long syntax to short syntax string
                if (target.HasValue)
                {
                    var portString = published.HasValue ? $"{published}:{target}" : $"{target}";
                    if (!string.IsNullOrEmpty(hostIp))
                    {
                        portString = $"{hostIp}:{portString}";
                    }
                    if (!string.IsNullOrEmpty(protocol))
                    {
                        portString = $"{portString}/{protocol}";
                    }
                    result.Add(portString);
                }
            }
            else
            {
                parser.MoveNext();
            }
        }

        parser.MoveNext(); // Skip SequenceEnd
        return result;
    }

    public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer serializer)
    {
        if (value is List<string> ports)
        {
            emitter.Emit(new SequenceStart(null, null, false, SequenceStyle.Block));
            foreach (var port in ports)
            {
                emitter.Emit(new Scalar(port));
            }
            emitter.Emit(new SequenceEnd());
        }
    }
}

/// <summary>
/// Converts Docker Compose volumes from both short and long formats.
/// Supports both short syntax (e.g., "./source:/target:ro") and long syntax (type, source, target properties).
/// </summary>
internal class VolumesListTypeConverter : IYamlTypeConverter
{
    public bool Accepts(Type type)
    {
        return type == typeof(List<Resources.ServiceNodes.Volume>);
    }

    public object? ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
    {
        var result = new List<Resources.ServiceNodes.Volume>();

        if (parser.Current is not SequenceStart)
        {
            return result;
        }

        parser.MoveNext();
        while (parser.Current is not SequenceEnd)
        {
            if (parser.Current is Scalar scalar)
            {
                // Short syntax: "./source:/target" or "/target" or "./source:/target:ro"
                var volume = ParseShortSyntax(scalar.Value);
                if (volume is not null)
                {
                    result.Add(volume);
                }
                parser.MoveNext();
            }
            else if (parser.Current is MappingStart)
            {
                // Long syntax: type, source, target, etc.
                var volume = (Resources.ServiceNodes.Volume?)rootDeserializer(typeof(Resources.ServiceNodes.Volume));
                if (volume is not null)
                {
                    result.Add(volume);
                }
            }
            else
            {
                parser.MoveNext();
            }
        }
        parser.MoveNext(); // Skip SequenceEnd

        return result;
    }

    private static Resources.ServiceNodes.Volume? ParseShortSyntax(string volumeString)
    {
        if (string.IsNullOrWhiteSpace(volumeString))
        {
            return null;
        }

        var volume = new Resources.ServiceNodes.Volume
        {
            Name = string.Empty // Short syntax doesn't have a name
        };
        
        // Parse: [SOURCE:]TARGET[:MODE]
        var parts = volumeString.Split(':');
        
        if (parts.Length == 1)
        {
            // Just a target path (anonymous volume)
            volume.Target = parts[0];
            volume.Type = "volume";
        }
        else if (parts.Length == 2)
        {
            // SOURCE:TARGET
            volume.Source = parts[0];
            volume.Target = parts[1];
            // Determine type: if source starts with . or / it's a bind mount, otherwise it's a named volume
            volume.Type = parts[0].StartsWith('.') || parts[0].StartsWith('/') ? "bind" : "volume";
        }
        else if (parts.Length >= 3)
        {
            // SOURCE:TARGET:MODE (or possibly SOURCE:TARGET:ro or SOURCE:TARGET:rw,etc)
            volume.Source = parts[0];
            volume.Target = parts[1];
            volume.Type = parts[0].StartsWith('.') || parts[0].StartsWith('/') ? "bind" : "volume";
            
            // Parse mode options (ro, rw, etc.)
            for (int i = 2; i < parts.Length; i++)
            {
                if (parts[i] == "ro")
                {
                    volume.ReadOnly = true;
                }
            }
        }

        return volume;
    }

    public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer serializer)
    {
        if (value is List<Resources.ServiceNodes.Volume> volumes)
        {
            serializer(volumes);
        }
    }
}

/// <summary>
/// Type converter for Docker Compose build configuration that handles both short (string) and long (object) syntax.
/// </summary>
internal sealed class BuildTypeConverter : IYamlTypeConverter
{
    public bool Accepts(Type type) => type == typeof(Build);

    public object? ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
    {
        if (parser.TryConsume<Scalar>(out var scalar))
        {
            // Short syntax: just a context path string
            return new Build { Context = scalar.Value };
        }

        if (parser.Current is MappingStart)
        {
            // Long syntax: full Build object
            return rootDeserializer(typeof(Build));
        }

        return null;
    }

    public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer serializer)
    {
        if (value is Build build)
        {
            // If only context is set, use short syntax
            if (!string.IsNullOrEmpty(build.Context) &&
                string.IsNullOrEmpty(build.Dockerfile) &&
                string.IsNullOrEmpty(build.Target) &&
                build.Args.Count == 0 &&
                build.CacheFrom.Count == 0 &&
                build.Labels.Count == 0)
            {
                emitter.Emit(new Scalar(build.Context));
            }
            else
            {
                // Use long syntax
                serializer(build, typeof(Build));
            }
        }
    }
}
