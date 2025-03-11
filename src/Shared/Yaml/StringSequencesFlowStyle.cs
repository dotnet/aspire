// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.EventEmitters;

namespace Aspire.Hosting.Yaml;

/// <summary>
/// A specialized implementation of the <see cref="ChainedEventEmitter"/> class
/// that modifies the sequence style of string array sequences in YAML serialization.
/// </summary>
/// <remarks>
/// <see cref="StringSequencesFlowStyle"/> ensures that when serializing sequences
/// of strings (i.e., string arrays), the sequence is emitted using the flow style
/// rather than the default block style.
/// This is achieved by intercepting <see cref="SequenceStartEventInfo"/> for string arrays
/// and altering its style property before emitting it to the next emitter in the chain.
/// </remarks>
internal sealed class StringSequencesFlowStyle(IEventEmitter nextEmitter) : ChainedEventEmitter(nextEmitter)
{
    /// <summary>
    /// Emits a <see cref="SequenceStartEventInfo"/> during YAML serialization.
    /// Modifies sequences of string arrays to use a flow style instead of the default block style.
    /// </summary>
    /// <param name="eventInfo">The event information for the sequence start event to be emitted.</param>
    /// <param name="emitter">The emitter used to write the serialized YAML output.</param>
    public override void Emit(SequenceStartEventInfo eventInfo, IEmitter emitter)
    {
        if (typeof(string[]) == eventInfo.Source.Type)
        {
            eventInfo = new(eventInfo.Source)
            {
                Style = SequenceStyle.Flow,
            };
        }

        nextEmitter.Emit(eventInfo, emitter);
    }
}
