# Log categories, activity source names and metric names

Aspire.Azure.Data.Tables:
- Log categories:
  - "Azure.Core"
  - "Azure.Identity"
- Activity source names:
  - "Azure.Data.Tables.*"
- Metric names:
  - none (currently not supported by the Azure SDK)

Aspire.Azure.Messaging.ServiceBus:
- Log categories:
  - "Azure.Core"
  - "Azure.Identity"
  - "Azure.Messaging.ServiceBus"
- Activity source names:
  - "Azure.Messaging.ServiceBus.*"
  - "Azure.Messaging.ServiceBus"
- Metric names:
  - none (currently not supported by the Azure SDK)

Aspire.Azure.Security.KeyVault:
- Log categories:
  - "Azure.Core"
  - "Azure.Identity"
- Activity source names:
  - "Azure.Security.KeyVault.Secrets.*"
- Metric names:
  - none (currently not supported by the Azure SDK)

Aspire.Azure.Storage.Blobs:
- Log categories:
  - "Azure.Core"
  - "Azure.Identity"
- Activity source names:
  - "Azure.Storage.Blobs.*"
- Metric names:
  - none (currently not supported by the Azure SDK)

Aspire.Azure.Storage.Queues:
- Log categories:
  - "Azure.Core"
  - "Azure.Identity"
- Activity source names:
  - "Azure.Storage.Queues.*"
- Metric names:
  - none (currently not supported by the Azure SDK)

Aspire.Microsoft.Azure.Cosmos:
- Log categories:
  - "Azure-Cosmos-Operation-Request-Diagnostics"
- Activity source names:
  - "Azure.Cosmos.Operation"
- Metric names:

Aspire.Microsoft.Data.SqlClient:
- Log categories:
  - none (the client does not provide an easy way to integrate it with logger factory)
- Activity source names:
  - "OpenTelemetry.Instrumentation.SqlClient"
- Metric names:
  - "Microsoft.Data.SqlClient.EventSource"
    - "active-hard-connections"
    - "hard-connects"
    - "hard-disconnects"
    - "active-soft-connects"
    - "soft-connects"
    - "soft-disconnects"
    - "number-of-non-pooled-connections"
    - "number-of-pooled-connections"
    - "number-of-active-connection-pool-groups"
    - "number-of-inactive-connection-pool-groups"
    - "number-of-active-connection-pools"
    - "number-of-inactive-connection-pools"
    - "number-of-active-connections"
    - "number-of-free-connections"
    - "number-of-stasis-connections"
    - "number-of-reclaimed-connections"

Aspire.MongoDB.Driver:
- Log categories:
  - "MongoDB"
  - "MongoDB.Command"
  - "MongoDB.SDAM"
  - "MongoDB.ServerSelection"
  - "MongoDB.Connection"
  - "MongoDB.Internal"
- Activity source names:
  - "MongoDB.Driver.Core.Extensions.DiagnosticSources"
- Metric names:
  - none

Aspire.Microsoft.EntityFrameworkCore.Cosmos:
- Log categories:
  - "Azure-Cosmos-Operation-Request-Diagnostics"
  - "Microsoft.EntityFrameworkCore.ChangeTracking",
  - "Microsoft.EntityFrameworkCore.Database.Command",
  - "Microsoft.EntityFrameworkCore.Infrastructure",
  - "Microsoft.EntityFrameworkCore.Query",
- Activity source names:
  - "Azure.Cosmos.Operation"
  - "OpenTelemetry.Instrumentation.EntityFrameworkCore"
- Metric names:
  - "Microsoft.EntityFrameworkCore":
    - "ec_Microsoft_EntityFrameworkCore_active_db_contexts"
    - "ec_Microsoft_EntityFrameworkCore_total_queries"
    - "ec_Microsoft_EntityFrameworkCore_queries_per_second"
    - "ec_Microsoft_EntityFrameworkCore_total_save_changes"
    - "ec_Microsoft_EntityFrameworkCore_save_changes_per_second"
    - "ec_Microsoft_EntityFrameworkCore_compiled_query_cache_hit_rate"
    - "ec_Microsoft_Entity_total_execution_strategy_operation_failures"
    - "ec_Microsoft_E_execution_strategy_operation_failures_per_second"
    - "ec_Microsoft_EntityFramew_total_optimistic_concurrency_failures"
    - "ec_Microsoft_EntityF_optimistic_concurrency_failures_per_second"

Aspire.Microsoft.EntityFrameworkCore.SqlServer:
- Log categories:
  - "Microsoft.EntityFrameworkCore.ChangeTracking"
  - "Microsoft.EntityFrameworkCore.Database.Command"
  - "Microsoft.EntityFrameworkCore.Database.Connection"
  - "Microsoft.EntityFrameworkCore.Database.Transaction"
  - "Microsoft.EntityFrameworkCore.Infrastructure"
  - "Microsoft.EntityFrameworkCore.Migrations"
  - "Microsoft.EntityFrameworkCore.Model"
  - "Microsoft.EntityFrameworkCore.Model.Validation"
  - "Microsoft.EntityFrameworkCore.Query"
  - "Microsoft.EntityFrameworkCore.Update"
- Activity source names:
  - "OpenTelemetry.Instrumentation.EntityFrameworkCore"
- Metric names:
  - "Microsoft.EntityFrameworkCore":
    - "ec_Microsoft_EntityFrameworkCore_active_db_contexts"
    - "ec_Microsoft_EntityFrameworkCore_total_queries"
    - "ec_Microsoft_EntityFrameworkCore_queries_per_second"
    - "ec_Microsoft_EntityFrameworkCore_total_save_changes"
    - "ec_Microsoft_EntityFrameworkCore_save_changes_per_second"
    - "ec_Microsoft_EntityFrameworkCore_compiled_query_cache_hit_rate"
    - "ec_Microsoft_Entity_total_execution_strategy_operation_failures"
    - "ec_Microsoft_E_execution_strategy_operation_failures_per_second"
    - "ec_Microsoft_EntityFramew_total_optimistic_concurrency_failures"
    - "ec_Microsoft_EntityF_optimistic_concurrency_failures_per_second"

Aspire.MySqlConnector:
- Log categories:
  - "MySqlConnector.ConnectionPool"
  - "MySqlConnector.MySqlBulkCopy"
  - "MySqlConnector.MySqlCommand"
  - "MySqlConnector.MySqlConnection"
  - "MySqlConnector.MySqlDataSource"
- Activity source names:
  - "MySqlConnector"
- Metric names:
  - "MySqlConnector":
    - "db.client.connections.create_time"
    - "db.client.connections.use_time"
    - "db.client.connections.wait_time"
    - "db.client.connections.idle.max"
    - "db.client.connections.idle.min"
    - "db.client.connections.max"
    - "db.client.connections.pending_requests"
    - "db.client.connections.timeouts"
    - "db.client.connections.usage"

Aspire.Npgsql:
- Log categories:
  - "Npgsql.Command"
  - "Npgsql.Connection"
  - "Npgsql.Copy"
  - "Npgsql.Exception"
  - "Npgsql.Replication"
  - "Npgsql.Transaction"
- Activity source names:
  - "Npgsql"
- Metric names:
  - "Npgsql":
    - "db.client.commands.bytes_read"
    - "db.client.commands.bytes_written"
    - "db.client.commands.duration"
    - "db.client.commands.executing"
    - "db.client.commands.failed"
    - "db.client.connections.create_time"
    - "db.client.connections.max"
    - "db.client.connections.pending_requests"
    - "db.client.connections.timeouts"
    - "db.client.connections.usage"

Aspire.Npgsql.EntityFrameworkCore.PostgreSQL:
- Log categories:
  - "Microsoft.EntityFrameworkCore.ChangeTracking"
  - "Microsoft.EntityFrameworkCore.Database.Command"
  - "Microsoft.EntityFrameworkCore.Database.Connection"
  - "Microsoft.EntityFrameworkCore.Database.Transaction"
  - "Microsoft.EntityFrameworkCore.Infrastructure"
  - "Microsoft.EntityFrameworkCore.Migrations"
  - "Microsoft.EntityFrameworkCore.Model"
  - "Microsoft.EntityFrameworkCore.Model.Validation"
  - "Microsoft.EntityFrameworkCore.Query"
  - "Microsoft.EntityFrameworkCore.Update"
- Activity source names:
  - "Npgsql"
- Metric names:
  - "Microsoft.EntityFrameworkCore":
    - "ec_Microsoft_EntityFrameworkCore_active_db_contexts"
    - "ec_Microsoft_EntityFrameworkCore_total_queries"
    - "ec_Microsoft_EntityFrameworkCore_queries_per_second"
    - "ec_Microsoft_EntityFrameworkCore_total_save_changes"
    - "ec_Microsoft_EntityFrameworkCore_save_changes_per_second"
    - "ec_Microsoft_EntityFrameworkCore_compiled_query_cache_hit_rate"
    - "ec_Microsoft_Entity_total_execution_strategy_operation_failures"
    - "ec_Microsoft_E_execution_strategy_operation_failures_per_second"
    - "ec_Microsoft_EntityFramew_total_optimistic_concurrency_failures"
    - "ec_Microsoft_EntityF_optimistic_concurrency_failures_per_second"
  - "Npgsql":
    - "db.client.commands.bytes_read"
    - "db.client.commands.bytes_written"
    - "db.client.commands.duration"
    - "db.client.commands.executing"
    - "db.client.commands.failed"
    - "db.client.connections.create_time"
    - "db.client.connections.max"
    - "db.client.connections.pending_requests"
    - "db.client.connections.timeouts"
    - "db.client.connections.usage"

Aspire.Oracle.EntityFrameworkCore:
- Log categories:
  - "Microsoft.EntityFrameworkCore.ChangeTracking"
  - "Microsoft.EntityFrameworkCore.Database.Command"
  - "Microsoft.EntityFrameworkCore.Database.Connection"
  - "Microsoft.EntityFrameworkCore.Database.Transaction"
  - "Microsoft.EntityFrameworkCore.Infrastructure"
  - "Microsoft.EntityFrameworkCore.Migrations"
  - "Microsoft.EntityFrameworkCore.Model"
  - "Microsoft.EntityFrameworkCore.Model.Validation"
  - "Microsoft.EntityFrameworkCore.Query"
  - "Microsoft.EntityFrameworkCore.Update"
- Activity source names:
  - "OpenTelemetry.Instrumentation.EntityFrameworkCore"
- Metric names:
  - "Microsoft.EntityFrameworkCore":
    - "ec_Microsoft_EntityFrameworkCore_active_db_contexts"
    - "ec_Microsoft_EntityFrameworkCore_total_queries"
    - "ec_Microsoft_EntityFrameworkCore_queries_per_second"
    - "ec_Microsoft_EntityFrameworkCore_total_save_changes"
    - "ec_Microsoft_EntityFrameworkCore_save_changes_per_second"
    - "ec_Microsoft_EntityFrameworkCore_compiled_query_cache_hit_rate"
    - "ec_Microsoft_Entity_total_execution_strategy_operation_failures"
    - "ec_Microsoft_E_execution_strategy_operation_failures_per_second"
    - "ec_Microsoft_EntityFramew_total_optimistic_concurrency_failures"
    - "ec_Microsoft_EntityF_optimistic_concurrency_failures_per_second"

Aspire.RabbitMQ.Client:
- Log categories:
  - "RabbitMQ.Client"
- Activity source names:
  - "Aspire.RabbitMQ.Client"
- Metric names:
  - none (currently not supported by RabbitMQ.Client library)

Aspire.StackExchange.Redis:
- Log categories:
  - "Aspire.StackExchange.Redis" (this name is defined by our component, we can change it)
- Activity source names:
  - "OpenTelemetry.Instrumentation.StackExchangeRedis"
- Metric names:
  - none (currently not supported by StackExchange.Redis library)

Aspire.StackExchange.Redis.DistributedCaching:
- Everything from `Aspire.StackExchange.Redis` plus:
- Log categories:
  - "Microsoft.Extensions.Caching.StackExchangeRedis"

Aspire.StackExchange.Redis.OutputCaching:
- Everything from `Aspire.StackExchange.Redis` plus:
- Log categories:
  - "Microsoft.AspNetCore.OutputCaching.StackExchangeRedis"
