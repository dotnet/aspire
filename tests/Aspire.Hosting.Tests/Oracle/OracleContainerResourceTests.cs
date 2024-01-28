// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Aspire.Hosting.Utils;
using Oracle.ManagedDataAccess.Client;
using Xunit;

namespace Aspire.Hosting.Tests.Oracle;

public class OracleContainerResourceTests
{
    [Theory()]
    [InlineData(["password", "user id=system;password=\"password\";data source=myserver:1521;"])]
    [InlineData(["mypasswordwitha\"inthemiddle", "user id=system;password=\"mypasswordwitha\"\"inthemiddle\";data source=myserver:1521;"])]
    [InlineData(["mypasswordwitha\"attheend\"", "user id=system;password=\"mypasswordwitha\"\"attheend\"\"\";data source=myserver:1521;"])]
    [InlineData(["\"mypasswordwitha\"atthestart", "user id=system;password=\"\"\"mypasswordwitha\"\"atthestart\";data source=myserver:1521;"])]
    [InlineData(["mypasswordwitha'inthemiddle", "user id=system;password=\"mypasswordwitha'inthemiddle\";data source=myserver:1521;"])]
    [InlineData(["mypasswordwitha'attheend'", "user id=system;password=\"mypasswordwitha'attheend'\";data source=myserver:1521;"])]
    [InlineData(["'mypasswordwitha'atthestart", "user id=system;password=\"'mypasswordwitha'atthestart\";data source=myserver:1521;"])]
    public void TestEscapeSequencesForPassword(string password, string expectedConnectionString)
    {
        var connectionStringTemplate = "user id=system;password=\"{0}\";data source=myserver:1521;";
        var escapedPassword = PasswordUtil.EscapePassword(password);
        var actualConnectionString = string.Format(CultureInfo.InvariantCulture, connectionStringTemplate, escapedPassword);

        var builder = new OracleConnectionStringBuilder(actualConnectionString);
        Assert.Equal(password, builder.Password);
        Assert.Equal(expectedConnectionString, actualConnectionString);
    }
}
