// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore.SqlServer.Storage.Internal;
using Microsoft.EntityFrameworkCore.Storage;

namespace Aspire.Microsoft.EntityFrameworkCore.SqlServer.Tests;

#pragma warning disable EF1001 // Internal EF Core API usage.
public class CustomExecutionStrategy : SqlServerExecutionStrategy
{
    public CustomExecutionStrategy(ExecutionStrategyDependencies dependencies) : base(dependencies)
    {
    }
}
#pragma warning restore EF1001 // Internal EF Core API usage.
