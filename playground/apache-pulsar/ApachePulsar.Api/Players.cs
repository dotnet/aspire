// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using DotPulsar;
using DotPulsar.Abstractions;
using DotPulsar.Exceptions;
using DotPulsar.Extensions;

public abstract class Player(
    IConsumerBuilder<string> consumerB,
    IProducerBuilder<string> producerB,
    MatchCoordinator coordinator,
    ILogger logger
) : BackgroundService
{
    private uint _receivedBalls;
    public uint ReceivedBalls => _receivedBalls;

    protected abstract string Move { get; }

    /// <summary>
    /// Kick the ball (message) into opponent field (topic)
    /// </summary>
    public async Task SmackTheBall(CancellationToken cancellationToken = default)
    {
        if (coordinator.MatchHalt)
        {
            logger.LogWarning("Match halted");
            return;
        }

        var producer = producerB.Create();

        logger.LogInformation("Sending: {message}", Move);

        await Task.Delay(700, cancellationToken); // add some sim...

        await producer.Send(new MessageMetadata(), Move, cancellationToken);
    }

    /// <summary>
    /// Observes your own field (topic) for players responses (messages) so you can respond back
    /// </summary>
    private async Task ReceiveBall(CancellationToken cancellation)
    {
        var consumer = consumerB.Create();
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

public sealed class PingPlayer(
    [FromKeyedServices(typeof(PingPlayer))] IConsumerBuilder<string> consumerB,
    [FromKeyedServices(typeof(PingPlayer))] IProducerBuilder<string> producerB,
    MatchCoordinator coordinator,
    ILogger<PingPlayer> logger
) : Player(consumerB, producerB, coordinator, logger)
{
    protected override string Move => "ping";
}

public sealed class PongPlayer(
    [FromKeyedServices(typeof(PongPlayer))] IConsumerBuilder<string> consumerB,
    [FromKeyedServices(typeof(PongPlayer))] IProducerBuilder<string> producerB,
    MatchCoordinator coordinator,
    ILogger<PongPlayer> logger
) : Player(consumerB, producerB, coordinator, logger)
{
    protected override string Move => "pong";
}

public sealed class MatchCoordinator(ILogger<MatchCoordinator> logger)
{
    public bool MatchHalt { get; private set; }

    public async Task HaltMatch()
    {
        MatchHalt = true;
        logger.LogWarning("Match halted, match will be able to resume after 3 seconds timeout");

        await Task.Delay(TimeSpan.FromSeconds(3));
        MatchHalt = false;
    }
}
