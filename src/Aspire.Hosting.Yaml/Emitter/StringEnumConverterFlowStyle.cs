// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Runtime.Serialization;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.EventEmitters;

namespace Aspire.Hosting.Yaml.Emitter;

/// <summary>
/// Represents a custom event emitter that converts enumeration values to their string representation with specific formatting rules for YAML serialization.
/// </summary>
/// <remarks>
/// This class is used to process enumeration types during YAML serialization. If the enumeration member has
/// a <see cref="EnumMemberAttribute"/> specified, the member's value is retrieved and used as its YAML representation.
/// Otherwise, the enumeration's name is used as the default string representation.
/// Additionally, it ensures the scalar style remains consistent with the source object's scalar style in YAML format.
/// </remarks>
/// <inheritDoc />
public sealed class StringEnumConverterFlowStyle(IEventEmitter nextEmitter) : ChainedEventEmitter(nextEmitter)
{
    /// <summary>
    /// Emits a scalar YAML event for an enumeration value, converting the value to its string representation with custom formatting.
    /// </summary>
    /// <param name="eventInfo">Contains information about the scalar event, including the source object and its scalar style.</param>
    /// <param name="emitter">The YAML emitter used to write the serialized output.</param>
    public override void Emit(ScalarEventInfo eventInfo, IEmitter emitter)
    {
        if (eventInfo.Source is {Type: { IsEnum: true } sourceType, Value: { } value})
        {
            var enumMember = sourceType.GetMember(value.ToString()!).FirstOrDefault();
            var yamlValue = enumMember?.GetCustomAttributes<EnumMemberAttribute>(true).Select(ema => ema.Value).FirstOrDefault() ?? value.ToString();

            eventInfo = new(new ObjectDescriptor(
                yamlValue,
                typeof(string),
                typeof(string),
                eventInfo.Source.ScalarStyle
            ));
        }

        nextEmitter.Emit(eventInfo, emitter);
    }
}
