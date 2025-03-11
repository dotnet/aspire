// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Aspire.Hosting.Yaml.Converters;

/// <summary>
/// Provides a YAML type converter specifically for types that implement <see cref="IYamlValueCollection"/>.
/// This converter handles serialization of YAML value collections into a sequence format.
/// </summary>
public sealed class YamlValueCollectionConverter : IYamlTypeConverter
{
    /// <summary>
    /// Determines whether the specified type is assignable from <see cref="IYamlValueCollection"/>.
    /// This method is used to evaluate if the given type can be handled by the YAML type converter.
    /// </summary>
    /// <param name="type">The type to check for compatibility with <see cref="IYamlValueCollection"/>.</param>
    /// <returns>
    /// Returns <c>true</c> if the specified type is assignable from <see cref="IYamlValueCollection"/>; otherwise, <c>false</c>.
    /// </returns>
    public bool Accepts(Type type) => typeof(IYamlValueCollection).IsAssignableFrom(type);

    /// <summary>
    /// Reads YAML data from the provided parser and deserializes it into an object of the specified type.
    /// This method processes the YAML sequence format and converts it into an appropriate object representation using the root deserializer.
    /// </summary>
    /// <param name="parser">The YAML parser used to read YAML data.</param>
    /// <param name="type">The expected type of the object to deserialize.</param>
    /// <param name="rootDeserializer">The function used to deserialize child properties or elements within the YAML structure.</param>
    /// <returns>
    /// The deserialized object of the specified type constructed from the YAML data.
    /// </returns>
    public object ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer) => throw new NotImplementedException();

    /// <summary>
    /// Serializes an object implementing <see cref="IYamlValueCollection"/> into a YAML sequence format.
    /// This method emits the contents of the collection as sequence items, where each item
    /// is either a key-value pair (<see cref="IYamlKeyValue"/>) or a standalone key (<see cref="IYamlKey"/>).
    /// </summary>
    /// <param name="emitter">An implementation of <see cref="IEmitter"/> used to write YAML events.</param>
    /// <param name="value">The object to serialize, expected to implement <see cref="IYamlValueCollection"/>.</param>
    /// <param name="type">The type of the object to serialize.</param>
    /// <param name="serializer">A delegate used to serialize child objects that are not directly handled by this method.</param>
    public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer serializer)
    {
        if (value is not IYamlValueCollection valueCollection)
        {
            return;
        }

        emitter.Emit(new SequenceStart(AnchorName.Empty, TagName.Empty, false, SequenceStyle.Block));

        foreach (var item in valueCollection)
        {
            switch (item)
            {
                case IYamlKeyValue keyValue:
                    emitter.Emit(new Scalar(AnchorName.Empty, TagName.Empty, $"{keyValue.Key}={keyValue.Value}", ScalarStyle.DoubleQuoted, true, false));
                    continue;
                case IYamlKey key:
                    emitter.Emit(new Scalar(AnchorName.Empty, TagName.Empty, key.Key, ScalarStyle.DoubleQuoted, true, false));
                    break;
            }
        }

        emitter.Emit(new SequenceEnd());
    }
}
