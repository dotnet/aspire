// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Components.Common.Tests;
using MongoDB.Bson;
using MongoDB.Driver;
using Testcontainers.MongoDb;
using Xunit;

namespace Aspire.MongoDB.Driver.Tests;

public sealed class MongoDbContainerFixture : IAsyncLifetime
{
    public MongoDbContainer? Container { get; private set; }

    public string GetConnectionString() => Container?.GetConnectionString() ??
        throw new InvalidOperationException("The test container was not initialized.");

    public async Task InitializeAsync()
    {
        if (RequiresDockerTheoryAttribute.IsSupported)
        {
            Container = new MongoDbBuilder()
                .WithImage("mongo:7.0.5")
                .Build();
            await Container.StartAsync();

            // Create `test_db` database with user:mongo pwd:mongo
            var mongoClient = new MongoClient(Container.GetConnectionString());
            var createUserCommand = new BsonDocumentCommand<BsonDocument>(BsonDocument.Parse("""
            {
               createUser: "mongo",
               pwd: "mongo",
               roles: [ { role: 'readWrite', db: 'test_db' } ]
            }
            """));
            await mongoClient.GetDatabase("test_db")
                .RunCommandAsync(createUserCommand);
        }
    }

    public async Task DisposeAsync()
    {
        if (Container is not null)
        {
            await Container.DisposeAsync();
        }
    }
}
