// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Aspire.Oracle.ManagedDataAccess.Core.Tests;

public class ConfigurationTests
{
    [Fact]
    public void ConnectionStringIsNullByDefault()
    => Assert.Null(new OracleManagedDataAccessCoreSettings().ConnectionString);

    [Fact]
    public void HealthCheckIsEnabledByDefault()
        => Assert.True(new OracleManagedDataAccessCoreSettings().HealthChecks);

    [Fact]
    public void TracingIsEnabledByDefault()
        => Assert.True(new OracleManagedDataAccessCoreSettings().Tracing);
}
