// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using DotPulsar;
using DotPulsar.Abstractions;
using DotPulsar.Exceptions;
using DotPulsar.Extensions;

/// <summary>
/// Ping player produces pings
/// He receives pongs from Pong player
/// </summary>
public sealed class PingPlayer(IConsumerBuilder<string> consumerBuilder, IProducerBuilder<string> producerBuilder, ILogger logger)
    : Player(consumerBuilder, producerBuilder, logger)
{
    protected override string Move => "ping";
}

/// <summary>
/// Pong player produces pongs
/// He receives pings from Ping player
/// </summary>
public sealed class PongPlayer(IConsumerBuilder<string> consumerBuilder, IProducerBuilder<string> producerBuilder, ILogger logger)
    : Player(consumerBuilder, producerBuilder, logger)
{
    protected override string Move => "pong";
}

public abstract class Player(
    IConsumerBuilder<string> consumerBuilder,
    IProducerBuilder<string> producerBuilder,
    ILogger logger
) : BackgroundService
{
    protected abstract string Move { get; }

    private uint _receivedBalls;
    public uint ReceivedBalls => _receivedBalls;

    private readonly Lazy<IProducer<string>> _producer = new(producerBuilder.Create);

    /// <summary>
    /// Kick the ball (message) into opponent field (topic)
    /// </summary>
    public async Task SmackTheBall(CancellationToken cancellation)
    {
        logger.LogInformation("Responding: {message}", Move);

        await Task.Delay(700, cancellation); // add some sim...

        await _producer.Value.Send(new MessageMetadata(), Move, cancellation);
    }

    /// <summary>
    /// Observes your own field (topic) for players responses (messages) so you can respond back
    /// </summary>
    private async Task ReceiveBall(CancellationToken cancellation)
    {
        var consumer = consumerBuilder.Create();
        await foreach (var message in consumer.Messages(cancellation))
        {
            logger.LogInformation("Received: {message}", message);

            Interlocked.Increment(ref _receivedBalls);

            await SmackTheBall(cancellation);
        }
    }

    // Listener
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var attempt = 0;
        while (true)
        {
            try
            {
                await ReceiveBall(stoppingToken);
            }
            catch (DotPulsarException e)
            {
                logger.LogWarning("Pulsar is still warming up, retry connection attempt {attempt}.", ++attempt);
                logger.LogDebug(e, "Pulsar faulted");

                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }
}
