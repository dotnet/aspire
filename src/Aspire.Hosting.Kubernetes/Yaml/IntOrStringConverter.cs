// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Kubernetes.Resources;
using YamlDotNet.Core;
using YamlDotNet.Serialization;

namespace Aspire.Hosting.Kubernetes.Yaml;

/// <summary>
/// Provides a custom YAML type converter that facilitates serialization
/// and deserialization of objects of type <see cref="Int32OrStringV1"/>.
/// This converter supports both integers and strings.
/// </summary>
public class IntOrStringYamlConverter : IYamlTypeConverter
{
    /// <summary>
    /// Determines whether the given type is supported by this YAML type converter.
    /// </summary>
    /// <param name="type">The type to check for compatibility with the YAML converter.</param>
    /// <returns>Returns true if the specified type is <see cref="Int32OrStringV1"/>, otherwise false.</returns>
    public bool Accepts(Type type)
    {
        return type == typeof(Int32OrStringV1);
    }

    /// <summary>
    /// Reads a YAML scalar from the parser and converts it into an instance of <see cref="Int32OrStringV1"/>.
    /// </summary>
    /// <param name="parser">The YAML parser to read the scalar value from.</param>
    /// <param name="type">The target type for deserialization, expected to be <see cref="Int32OrStringV1"/>.</param>
    /// <param name="rootDeserializer">The root deserializer used for handling nested deserialization.</param>
    /// <returns>Returns an instance of <see cref="Int32OrStringV1"/> constructed from the parsed scalar value.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the current YAML event is not a scalar.</exception>
    public object? ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
    {
        if (parser.Current is not YamlDotNet.Core.Events.Scalar scalar)
        {
            throw new InvalidOperationException(parser.Current?.ToString());
        }

        var value = scalar.Value;
        parser.MoveNext();

        return string.IsNullOrEmpty(value) ? null : new Int32OrStringV1(value);
    }

    /// <summary>
    /// Writes the given object to the provided YAML emitter using the appropriate format.
    /// </summary>
    /// <param name="emitter">The emitter used to write the YAML output.</param>
    /// <param name="value">The object to be serialized. Expected to be of type <see cref="Int32OrStringV1"/>.</param>
    /// <param name="type">The type of the object being serialized.</param>
    /// <param name="serializer">The serializer to be used for complex object serialization.</param>
    /// <exception cref="InvalidOperationException">Thrown when the provided value is not of type <see cref="Int32OrStringV1"/>.</exception>
    public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer serializer)
    {
        if (value is not Int32OrStringV1 obj)
        {
            throw new InvalidOperationException($"Expected {nameof(Int32OrStringV1)} but got {value?.GetType()}");
        }

        var val = obj.Value ?? string.Empty;

        serializer(val);
    }
}
