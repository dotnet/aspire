// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using DotPulsar;
using DotPulsar.Abstractions;
using DotPulsar.Extensions;

public static class PlayerTopicFactory
{
    public const string TopicPrefix = "persistent://public/default";

    public static string GetTopic(string player) => $"{TopicPrefix}/{player}";

    public static string GetOpponentTopic(string player) => player switch
    {
        nameof(PingPlayer) => GetTopic(nameof(PongPlayer)),
        nameof(PongPlayer) => GetTopic(nameof(PingPlayer)),
        _ => throw new ArgumentOutOfRangeException(nameof(player), player, null)
    };
}

public static class PlayerRegistrationExtensions
{
    public static Type PlayerKey<T>() where T : Player => typeof(T);

    public static string PlayerName<T>() where T : Player => PlayerKey<T>().Name;

    public static void Register<T>(this IServiceCollection services, IPulsarClient client)
        where T : Player
    {
        var player = PlayerName<T>();
        var opponentField = PlayerTopicFactory.GetOpponentTopic(player);
        var playerField = PlayerTopicFactory.GetTopic(player);

        // Produce player move (message) into opponent field (topic)
        services.AddKeyedSingleton(player, (_, _) => client
            .NewProducer(Schema.String)
            .ProducerName(player)
            .Topic(opponentField)
        );

        // Listen your field (topic) for player move (message) so you can respond back
        services.AddKeyedSingleton(player, (_, _) => client
            .NewConsumer(Schema.String)
            .ConsumerName(player)
            .Topic(playerField)
            .SubscriptionName(player)
        );

        services.AddSingleton(Create<T>);
        services.AddHostedService(sp => sp.GetRequiredService<T>());
    }

    public static T Create<T>(this IServiceProvider sp)
        where T : Player
    {
        var player = PlayerName<T>();
        return (T)Activator.CreateInstance(
            typeof(T),
            sp.GetRequiredKeyedService<IConsumerBuilder<string>>(player),
            sp.GetRequiredKeyedService<IProducerBuilder<string>>(player),
            sp.GetRequiredService<ILoggerFactory>().CreateLogger(player)
        )!;
    }
}
