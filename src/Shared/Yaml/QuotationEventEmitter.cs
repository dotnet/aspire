// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.EventEmitters;

namespace Aspire.Hosting.Yaml;

/// <summary>
/// A custom implementation of <see cref="ChainedEventEmitter"/> that ensures all string
/// scalar values are emitted with double quotes in the YAML output.
/// </summary>
/// <remarks>
/// This event emitter modifies the behavior of scalar events by enforcing the use of double quotes
/// for string and object values during serialization. All other event types are delegated to the underlying emitter.
/// </remarks>
/// <example>
/// This class is useful in scenarios where quoted output is necessary to ensure compatibility
/// with specific YAML processing tools or to escape special characters in string values.
/// </example>
internal sealed class QuotationEventEmitter(IEventEmitter nextEmitter) : ChainedEventEmitter(nextEmitter)
{
    /// <summary>
    /// Emits a scalar event during YAML serialization, ensuring that string and object values are
    /// serialized with double quotes. Delegates other event types to the underlying emitter.
    /// </summary>
    /// <param name="eventInfo">Provides information about the scalar event being emitted, including its type and value.</param>
    /// <param name="emitter">The emitter used to write the serialized YAML output.</param>
    public override void Emit(ScalarEventInfo eventInfo, IEmitter emitter)
    {
        if (eventInfo.Source.StaticType == typeof(string) || eventInfo.Source.StaticType == typeof(object))
        {
            eventInfo.Style = ScalarStyle.DoubleQuoted;
        }

        base.Emit(eventInfo, emitter);
    }

    /// <summary>
    /// Emits a scalar, mapping start, or mapping end event during the YAML serialization process.
    /// Customizes the emission of scalar events to ensure string and object values are wrapped in double quotes,
    /// while delegating all other event emission to the next emitter in the chain.
    /// </summary>
    /// <param name="eventInfo">Metadata describing the event being emitted, including type and style information.</param>
    /// <param name="emitter">The YAML emitter responsible for writing the event data to the output.</param>
    public override void Emit(MappingStartEventInfo eventInfo, IEmitter emitter) => nextEmitter.Emit(eventInfo, emitter);

    /// <summary>
    /// Emits YAML scalar or mapping end events while customizing behavior for certain event types.
    /// Ensures proper delegation to the underlying emitter for unmodified event handling.
    /// </summary>
    /// <param name="eventInfo">Metadata describing the YAML scalar or mapping end event being emitted.</param>
    /// <param name="emitter">The object responsible for writing the YAML event to the output stream.</param>
    public override void Emit(MappingEndEventInfo eventInfo, IEmitter emitter) => nextEmitter.Emit(eventInfo, emitter);
}
