// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.MongoDB;
using MongoDB.Driver;
using Xunit;

namespace Aspire.Hosting.Tests.MongoDB;

public class MongoDBContainerResourceTests
{
    [Theory]
    [InlineData("password", "mongodb://root:password@myserver:1000/")]
    [InlineData("@abc!$", "mongodb://root:%40abc!$@myserver:1000/")]
    [InlineData("mypasswordwitha\"inthemiddle", "mongodb://root:mypasswordwitha\"inthemiddle@myserver:1000/")]
    [InlineData("mypasswordwitha\"attheend\"", "mongodb://root:mypasswordwitha\"attheend\"@myserver:1000/")]
    [InlineData("\"mypasswordwitha\"atthestart", "mongodb://root:\"mypasswordwitha\"atthestart@myserver:1000/")]
    [InlineData("mypasswordwitha'inthemiddle", "mongodb://root:mypasswordwitha'inthemiddle@myserver:1000/")]
    [InlineData("mypasswordwitha'attheend'", "mongodb://root:mypasswordwitha'attheend'@myserver:1000/")]
    [InlineData("'mypasswordwitha'atthestart", "mongodb://root:'mypasswordwitha'atthestart@myserver:1000/")]
    public void TestSpecialCharactersAndEscapeForPassword(string password, string expectedConnectionString)
    {
        var connectionString = new MongoDBConnectionStringBuilder()
            .WithServer("myserver")
            .WithPort(1000)
            .WithUserName("root")
            .WithPassword(password)
            .Build();

        Assert.NotNull(connectionString);

        var builder = MongoUrl.Create(connectionString);
        Assert.Equal(password, builder.Password);
        Assert.Equal(expectedConnectionString, connectionString);
    }
}
