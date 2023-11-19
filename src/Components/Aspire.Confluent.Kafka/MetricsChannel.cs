// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Channels;

namespace Aspire.Confluent.Kafka;

internal sealed class MetricsChannel
{
    private readonly Channel<string> _channel = Channel.CreateBounded<string>(new BoundedChannelOptions(10_000)
    {
        SingleReader = true,
        SingleWriter = false
    });

    public ChannelReader<string> Reader => _channel.Reader;
    public ChannelWriter<string> Writer => _channel.Writer;
}
