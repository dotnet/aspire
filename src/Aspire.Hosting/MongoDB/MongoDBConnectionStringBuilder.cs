// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.MongoDB;

internal class MongoDBConnectionStringBuilder
{
    private const string Scheme = "mongodb";

    private string? _server;
    private int _port;
    private string? _userName;
    private string? _password;

    public MongoDBConnectionStringBuilder WithServer(string server)
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(server, nameof(server));

        _server = server;

        return this;
    }

    public MongoDBConnectionStringBuilder WithPort(int port)
    {
        _port = port;

        return this;
    }

    public MongoDBConnectionStringBuilder WithUserName(string userName)
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(userName, nameof(userName));

        _userName = userName;

        return this;
    }

    public MongoDBConnectionStringBuilder WithPassword(string password)
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(password, nameof(password));

        _password = password;

        return this;
    }

    public string Build()
    {
        var builder = new UriBuilder
        {
            Scheme = Scheme,
            Host = _server,
            Port = _port,
            UserName = _userName,
            Password = _password
        };

        return builder.ToString();
    }
}
