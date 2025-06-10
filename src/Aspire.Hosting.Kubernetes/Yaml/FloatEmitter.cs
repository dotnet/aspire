// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.EventEmitters;

namespace Aspire.Hosting.Kubernetes.Yaml;

internal class FloatEmitter(IEventEmitter nextEmitter) : ChainedEventEmitter(nextEmitter)
{
    public override void Emit(ScalarEventInfo eventInfo, IEmitter emitter)
    {
        switch (eventInfo.Source.Value)
        {
            case double d:
                emitter.Emit(new Scalar(d.ToString("0.0######################", CultureInfo.InvariantCulture)));
                break;
            case float f:
                emitter.Emit(new Scalar(f.ToString("0.0######################", CultureInfo.InvariantCulture)));
                break;
            default:
                base.Emit(eventInfo, emitter);
                break;
        }
    }
}
