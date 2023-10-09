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

Aspire.Npgsql.EntityFrameworkCore.PostgreSQL:
- Log categories:
  - "Microsoft.EntityFrameworkCore.Infrastructure"
  - "Microsoft.EntityFrameworkCore.ChangeTracking"
  - "Microsoft.EntityFrameworkCore.Infrastructure"
  - "Microsoft.EntityFrameworkCore.Database.Command"
  - "Microsoft.EntityFrameworkCore.Query"
  - "Microsoft.EntityFrameworkCore.Database.Transaction"
  - "Microsoft.EntityFrameworkCore.Database.Connection"
  - "Microsoft.EntityFrameworkCore.Model"
  - "Microsoft.EntityFrameworkCore.Model.Validation"
  - "Microsoft.EntityFrameworkCore.Update"
  - "Microsoft.EntityFrameworkCore.Migrations"
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
    - "ec_Npgsql_bytes_written_per_second"
    - "ec_Npgsql_bytes_read_per_second"
    - "ec_Npgsql_commands_per_second"
    - "ec_Npgsql_total_commands"
    - "ec_Npgsql_current_commands"
    - "ec_Npgsql_failed_commands"
    - "ec_Npgsql_prepared_commands_ratio"
    - "ec_Npgsql_connection_pools"
    - "ec_Npgsql_multiplexing_average_commands_per_batch"
    - "ec_Npgsql_multiplexing_average_write_time_per_batch"

Aspire.Npgsql:
- Log categories:
  - "Npgsql.Connection"
  - "Npgsql.Command"
  - "Npgsql.Transaction"
  - "Npgsql.Copy"
  - "Npgsql.Replication"
  - "Npgsql.Exception"
- Activity source names:
  - "Npgsql"
- Metric names:
  - "Npgsql":
    - "ec_Npgsql_bytes_written_per_second"
    - "ec_Npgsql_bytes_read_per_second"
    - "ec_Npgsql_commands_per_second"
    - "ec_Npgsql_total_commands"
    - "ec_Npgsql_current_commands"
    - "ec_Npgsql_failed_commands"
    - "ec_Npgsql_prepared_commands_ratio"
    - "ec_Npgsql_connection_pools"
    - "ec_Npgsql_multiplexing_average_commands_per_batch"
    - "ec_Npgsql_multiplexing_average_write_time_per_batch"

Aspire.Microsoft.EntityFrameworkCore.SqlServer:
- Log categories:
  - "Microsoft.EntityFrameworkCore.Infrastructure"
  - "Microsoft.EntityFrameworkCore.ChangeTracking"
  - "Microsoft.EntityFrameworkCore.Infrastructure"
  - "Microsoft.EntityFrameworkCore.Database.Command"
  - "Microsoft.EntityFrameworkCore.Query"
  - "Microsoft.EntityFrameworkCore.Database.Transaction"
  - "Microsoft.EntityFrameworkCore.Database.Connection"
  - "Microsoft.EntityFrameworkCore.Model"
  - "Microsoft.EntityFrameworkCore.Model.Validation"
  - "Microsoft.EntityFrameworkCore.Update"
  - "Microsoft.EntityFrameworkCore.Migrations"
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
