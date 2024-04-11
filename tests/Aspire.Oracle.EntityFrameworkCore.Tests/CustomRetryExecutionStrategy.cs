// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore.Storage;
using Oracle.EntityFrameworkCore;

namespace Aspire.Oracle.EntityFrameworkCore.Tests;

#pragma warning disable EF1001 // Internal EF Core API usage.
public class CustomRetryExecutionStrategy : OracleRetryingExecutionStrategy
{
    public const int DefaultRetryCount = 123;

    public CustomRetryExecutionStrategy(ExecutionStrategyDependencies dependencies) : base(dependencies)
    {
    }

    public int RetryCount => DefaultRetryCount;
}
#pragma warning restore EF1001 // Internal EF Core API usage.
