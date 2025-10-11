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