// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore.Storage;
using Npgsql.EntityFrameworkCore.PostgreSQL.Storage.Internal;

namespace Aspire.Azure.Npgsql.EntityFrameworkCore.PostgreSQL.Tests;

#pragma warning disable EF1001 // Internal EF Core API usage.
public class CustomExecutionStrategy : NpgsqlExecutionStrategy
{
    public CustomExecutionStrategy(ExecutionStrategyDependencies dependencies) : base(dependencies)
    {
    }
}
#pragma warning restore EF1001 // Internal EF Core API usage.
