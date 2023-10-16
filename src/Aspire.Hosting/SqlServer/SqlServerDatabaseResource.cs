// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.SqlServer;

public class SqlServerDatabaseResource : ContainerResource, ISqlServerResource, IDistributedApplicationResourceWithParent<SqlServerResource>
{
    public SqlServerDatabaseResource(string name, SqlServerResource sqlServerContainer) : base(name)
    {
        Parent = sqlServerContainer;
    }

    public SqlServerResource Parent { get; }

    public string? GetConnectionString()
    {
        if (Parent.GetConnectionString() is { } connectionString)
        {
            return $"{connectionString}Database={Name}";
        }
        else
        {
            throw new DistributedApplicationException("Parent resource connection string was null.");
        }
    }
}
