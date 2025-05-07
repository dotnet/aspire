# .NET Aspire Components Progress for November

As part of the .NET Aspire November preview, we want to include a set of .NET Aspire Components which help developers build .NET Aspire applications. These components should follow the [.NET Aspire Component Requirements](#net-aspire-component-requirements). Bellow is a chart that shows the progress of each of the components we intend to ship, and their current stance against each of the requirements.

| .NET Aspire Component Name              | [Contains README](#contains-readme) | [Public API](#public-api) | [Configuration Schema](#json-schemaconfiguration) | [DI Services](#di-services) | [Logging](#logging) | [Tracing](#tracing) | [Metrics](#metrics) | [Health Checks](#health-checks) |
| --------------------------------------- | :---------------------------------: | :-----------------------: | :----------------------------------------------------: | :-------------------------: | :-----------------: | :-----------------: | :-----------------: | :-----------------------------: |
| Npgsql                                  |                  ✅                  |             ✅             |                           ✅                            |              ✅              |          ✅          |        ✅            |         ✅           |              ✅                  |
| Npgsql.EntityFrameworkCore.PostgreSQL   |                  ✅                  |             ✅             |                           ✅                            |              ✅              |          ✅          |        ✅            |         ✅           |              ✅                  |
| Microsoft.Azure.Cosmos                  |                  ✅                  |             ✅             |                           ✅                            |              ✅              |          ✅          |        ✅            |         ❌           |              ❌                  |
| Microsoft.Data.SqlClient                |                  ✅                  |             ✅             |                           ✅                            |              ✅              |          ❌          |        ✅            |         ❌           |              ✅                  |
| Microsoft.EntityFramework.Cosmos        |                  ✅                  |             ✅             |                           ✅                            |              ✅              |          ✅          |        ✅            |         ❌           |              ❌                  |
| Microsoft.EntityFrameworkCore.SqlServer |                  ✅                  |             ✅             |                           ✅                            |              ✅              |          ✅          |        ✅            |         ❌           |              ✅                  |
| MongoDB.Driver                          |                  ✅                  |             ✅             |                           ✅                            |              ✅              |          ✅          |        ✅            |         ❌           |              ✅                  |
| Azure.AI.OpenAI                         |                  ✅                  |             ✅             |                           ✅                            |              ✅              |          ✅          |        ✅            |         ✅           |              ❌                  |
| Azure.AppConfiguration                  |                  ✅                  |             ✅             |                           ✅                            |              ✅              |          ✅          |                      |         ❌           |                                  |
| Azure.Data.Tables                       |                  ✅                  |             ✅             |                           ✅                            |              ✅              |          ✅          |        ✅            |         ❌           |              ✅                  |
| Azure.Messaging.EventHubs               |                  ✅                  |             ✅             |                           ✅                            |              ✅              |          ✅          |        ✅            |         ❌           |                                  |
| Azure.Messaging.WebPubSub               |                  ✅                  |             ✅             |                           ✅                            |              ✅              |          ✅          |        ✅            |         ❌           |              ✅                  |
| Azure.Messaging.ServiceBus              |                  ✅                  |             ✅             |                           ✅                            |              ✅              |          ✅          |        ✅            |         ❌           |              ✅                  |
| Azure.Npgsql                            |                  ✅                  |             ✅             |                           ✅                            |              ✅              |          ✅          |        ✅            |         ✅           |              ✅                  |
| Azure.Npgsql.EntityFrameworkCore.PostgreSQL |              ✅                  |             ✅             |                           ✅                            |              ✅              |          ✅          |        ✅            |         ✅           |              ✅                  |
| Azure.Search.Documents                  |                  ✅                  |             ✅             |                           ✅                            |              ✅              |          ✅          |        ✅            |         ❌           |              ✅                  |
| Azure.Security.KeyVault                 |                  ✅                  |             ✅             |                           ✅                            |              ✅              |          ✅          |        ✅            |         ❌           |              ✅                  |
| Azure.Storage.Blobs                     |                  ✅                  |             ✅             |                           ✅                            |              ✅              |          ✅          |        ✅            |         ❌           |              ✅                  |
| Azure.Storage.Queues                    |                  ✅                  |             ✅             |                           ✅                            |              ✅              |          ✅          |        ✅            |         ❌           |              ✅                  |
| StackExchange.Redis                     |                  ✅                  |             ✅             |                           ✅                            |              ✅              |          ✅          |        ✅            |         ❌           |              ✅                  |
| StackExchange.Redis.DistributedCaching  |                  ✅                  |             ✅             |                           ✅                            |              ✅              |          ✅          |        ✅            |         ❌           |              ✅                  |
| StackExchange.Redis.OutputCaching       |                  ✅                  |             ✅             |                           ✅                            |              ✅              |          ✅          |        ✅            |         ❌           |              ✅                  |
| RabbitMQ                                |                  ✅                  |             ✅             |                           ✅                            |              ✅              |          ✅          |                      |         ❌           |              ✅                  |
| MySqlConnector                          |                  ✅                  |             ✅             |                           ✅                            |              ✅              |          ✅          |        ✅            |         ✅           |              ✅                  |
| OpenAI                                  |                  ✅                  |             ✅             |                           ✅                            |              ✅              |          ❌          |        ✅            |         ✅           |              ❌                  |
| Oracle.EntityFrameworkCore              |                  ✅                  |             ✅             |                           ✅                            |              ✅              |          ✅          |        ✅            |         ❌           |              ✅                  |
| Confluent.Kafka                         |                  ✅                  |             ✅             |                           ✅                            |              ✅              |          ✅          |        ❌            |         ✅           |              ✅                  |
| Pomelo.EntityFrameworkCore.MySql        |                  ✅                  |             ✅             |                           ✅                            |              ✅              |          ✅          |        ✅            |         ✅           |              ✅                  |
| NATS.Net                                |                  ✅                  |             ✅             |                           ✅                            |              ✅              |          ✅          |        ✅            |                      |              ✅                  |
| Seq                                     |                  ✅                  |             ✅             |                           ✅                            |              ✅              |          ✅          |        N/A           |        N/A           |              ✅                  |
| Qdrant.Client                           |                  ✅                  |             ✅             |                           ✅                            |              ✅              |          ✅          |                    |                   |                                |
| Milvus.Client                           |                  ✅                  |             ✅             |                           ✅                            |              ✅              |          ✅          |                    |                   |                    ✅          |
| Elastic.Clients.Elasticsearch           |                  ✅                  |             ✅             |                           ✅                            |              ✅              |                      |         ✅           |                       |              ✅                 |

Nomenclature used in the table above:

- ✅ - Requirement is met
- (blank) - Requirement hasn't been met yet
- ❌ - Requirement can't be met
- N/A - Requirement not applicable

## .NET Aspire Component Requirements

### Contains README

Each .NET Aspire component must contain a README.md file which is included in the package. This README should contain the component's main description, usage examples, and basic getting started documentation. The goal of this file is to contain everything a developer will need in the first 5 minutes. Finally, README should have a link pointed back to the full documentation of the component, which will include a list of logging categories used, tracing activity names, and Metric names. For a concrete example of a README file, please look [here](./Aspire.StackExchange.Redis/README.md).

### Public API

Each component must go through an API Review that will validate that the API shape proposed by the component is conforming to the guidelines. This also includes the [naming conventions](./README.md#naming).

### Json Schema/Configuration

Each component should provide a `sealed` `Settings` type as well as named configuration which will showcase the exposed settings for that specific component. This will also include the Logging section in which categories are listed and can be configured. For more information, please check out [the configuration best practices](./README.md#configuration)

### DI Services

Components must have extension methods which will "glue" the services with the DI container. For an example of this, please check out the `AspireRedisExtensions` class [here](./Aspire.StackExchange.Redis/AspireRedisExtensions.cs). The extension methods that are registering the main component's service to the container should be listed in the README file. The full list of registered services by the component should be included in the main component's documentation page.

### Health Checks

Aspire components should expose health checks enabling applications to track and respond to the remote service’s health. For more information, please check out [the health checks best practices](./README.md#health-checks).

### Telemetry

#### Logging

Components should produce Logs as part of them being cloud-ready. As part of the component's main documentation, it's a requirement to list out the different categories that it uses for logging. The list of categories doesn't need to be exhaustive, but it should include at least the logical level groupings used.

#### Tracing

Components should also produce tracing information as part of them being cloud-ready. As part of the component's main documentation, it's a requirement to list out the different activity names that it uses for tracing. It is not a requirement to include tag names.

#### Metrics

Components should also produce metrics as part of them being cloud-ready. As part of the component's main documentation, it's a requirement to list out the different metric names that it uses. It is not a requirement to also include dimensions.
