// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore;
using Npgsql.EntityFrameworkCore.PostgreSQL;

namespace Aspire.Npgsql.Tests;

public class WorkaroundToReadProtectedField : NpgsqlRetryingExecutionStrategy
{
    public WorkaroundToReadProtectedField(DbContext context) : base(context)
    {
    }

    public int RetryCount => base.MaxRetryCount;
}
