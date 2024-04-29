// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore;
using Oracle.EntityFrameworkCore;

namespace Aspire.Oracle.EntityFrameworkCore.Tests;

public class WorkaroundToReadProtectedField : OracleRetryingExecutionStrategy
{
    public WorkaroundToReadProtectedField(DbContext context) : base(context)
    {
    }

    public int RetryCount => base.MaxRetryCount;
}
