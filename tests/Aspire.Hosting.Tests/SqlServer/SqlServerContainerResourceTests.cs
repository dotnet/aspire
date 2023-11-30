// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Aspire.Hosting.Utils;
using Microsoft.Data.SqlClient;
using Xunit;

namespace Aspire.Hosting.Tests.SqlServer;

public class SqlServerContainerResourceTests
{
    [Theory()]
    [InlineData(["password", "Server=myserver;User ID=sa;Password=\"password\";"])]
    [InlineData(["mypasswordwitha\"inthemiddle", "Server=myserver;User ID=sa;Password=\"mypasswordwitha\"\"inthemiddle\";"])]
    [InlineData(["mypasswordwitha\"attheend\"", "Server=myserver;User ID=sa;Password=\"mypasswordwitha\"\"attheend\"\"\";"])]
    [InlineData(["\"mypasswordwitha\"atthestart", "Server=myserver;User ID=sa;Password=\"\"\"mypasswordwitha\"\"atthestart\";"])]
    [InlineData(["mypasswordwitha'inthemiddle", "Server=myserver;User ID=sa;Password=\"mypasswordwitha'inthemiddle\";"])]
    [InlineData(["mypasswordwitha'attheend'", "Server=myserver;User ID=sa;Password=\"mypasswordwitha'attheend'\";"])]
    [InlineData(["'mypasswordwitha'atthestart", "Server=myserver;User ID=sa;Password=\"'mypasswordwitha'atthestart\";"])]
    public void TestEscapeSequencesForPassword(string password, string expectedConnectionString)
    {
        var connectionStringTemplate = "Server=myserver;User ID=sa;Password=\"{0}\";";
        var escapedPassword = PasswordUtil.EscapePassword(password);
        var actualConnectionString = string.Format(CultureInfo.InvariantCulture, connectionStringTemplate, escapedPassword);

        var builder = new SqlConnectionStringBuilder(actualConnectionString);
        Assert.Equal(password, builder.Password);
        Assert.Equal(expectedConnectionString, actualConnectionString);
    }
}
