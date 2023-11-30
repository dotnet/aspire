// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Aspire.Hosting.Utils;
using Npgsql;
using Xunit;

namespace Aspire.Hosting.Tests.Postgres;

public class PostgresContainerResourceTests
{
    [Theory()]
    [InlineData(["password", "Host=myserver;Port=1000;Username=postgres;Password=\"password\";"])]
    [InlineData(["mypasswordwitha\"inthemiddle", "Host=myserver;Port=1000;Username=postgres;Password=\"mypasswordwitha\"\"inthemiddle\";"])]
    [InlineData(["mypasswordwitha\"attheend\"", "Host=myserver;Port=1000;Username=postgres;Password=\"mypasswordwitha\"\"attheend\"\"\";"])]
    [InlineData(["\"mypasswordwitha\"atthestart", "Host=myserver;Port=1000;Username=postgres;Password=\"\"\"mypasswordwitha\"\"atthestart\";"])]
    [InlineData(["mypasswordwitha'inthemiddle", "Host=myserver;Port=1000;Username=postgres;Password=\"mypasswordwitha'inthemiddle\";"])]
    [InlineData(["mypasswordwitha'attheend'", "Host=myserver;Port=1000;Username=postgres;Password=\"mypasswordwitha'attheend'\";"])]
    [InlineData(["'mypasswordwitha'atthestart", "Host=myserver;Port=1000;Username=postgres;Password=\"'mypasswordwitha'atthestart\";"])]
    public void TestEscapeSequencesForPassword(string password, string expectedConnectionString)
    {
        var connectionStringTemplate = "Host=myserver;Port=1000;Username=postgres;Password=\"{0}\";";
        var escapedPassword = PasswordUtil.EscapePassword(password);
        var actualConnectionString = string.Format(CultureInfo.InvariantCulture, connectionStringTemplate, escapedPassword);

        var builder = new NpgsqlConnectionStringBuilder(actualConnectionString);
        Assert.Equal(password, builder.Password);
        Assert.Equal(expectedConnectionString, actualConnectionString);
    }
}
