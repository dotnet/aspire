// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Aspire.Hosting.Utils;
using MySqlConnector;
using Xunit;

namespace Aspire.Hosting.Tests.MySql;

public class MySqlContainerResourceTests
{
    [Theory()]
    [InlineData(["password", "Server=myserver;Port=1000;User ID=root;Password=\"password\";"])]
    [InlineData(["mypasswordwitha\"inthemiddle", "Server=myserver;Port=1000;User ID=root;Password=\"mypasswordwitha\"\"inthemiddle\";"])]
    [InlineData(["mypasswordwitha\"attheend\"", "Server=myserver;Port=1000;User ID=root;Password=\"mypasswordwitha\"\"attheend\"\"\";"])]
    [InlineData(["\"mypasswordwitha\"atthestart", "Server=myserver;Port=1000;User ID=root;Password=\"\"\"mypasswordwitha\"\"atthestart\";"])]
    [InlineData(["mypasswordwitha'inthemiddle", "Server=myserver;Port=1000;User ID=root;Password=\"mypasswordwitha'inthemiddle\";"])]
    [InlineData(["mypasswordwitha'attheend'", "Server=myserver;Port=1000;User ID=root;Password=\"mypasswordwitha'attheend'\";"])]
    [InlineData(["'mypasswordwitha'atthestart", "Server=myserver;Port=1000;User ID=root;Password=\"'mypasswordwitha'atthestart\";"])]
    public void TestEscapeSequencesForPassword(string password, string expectedConnectionString)
    {
        var connectionStringTemplate = "Server=myserver;Port=1000;User ID=root;Password=\"{0}\";";
        var escapedPassword = PasswordUtil.EscapePassword(password);
        var actualConnectionString = string.Format(CultureInfo.InvariantCulture, connectionStringTemplate, escapedPassword);

        var builder = new MySqlConnectionStringBuilder(actualConnectionString);
        Assert.Equal(password, builder.Password);
        Assert.Equal(expectedConnectionString, actualConnectionString);
    }
}
