// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Components.Common.Tests;
using Testcontainers.Kafka;
using Xunit;

namespace Aspire.Confluent.Kafka.Tests;

public class KafkaContainerFixture : IAsyncLifetime
{
    public async Task InitializeAsync()
    {
        if (RequiresDockerAttribute.IsSupported)
        {
            Container = new KafkaBuilder()
                .WithImage($"{TestConstants.AspireTestContainerRegistry}/{KafkaBuilder.KafkaImage}")
                .Build();

            await Container.StartAsync();
        }
    }

    public KafkaContainer? Container { get; private set; }

    public async Task DisposeAsync()
    {
        if (Container is not null)
        {
            await Container.DisposeAsync();
        }
    }
}

[CollectionDefinition("Kafka Broker collection")]
public class KafkaBrokerCollection : ICollectionFixture<KafkaContainerFixture>
{

}
