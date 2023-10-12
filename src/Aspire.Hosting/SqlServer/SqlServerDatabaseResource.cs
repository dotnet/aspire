// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.SqlServer;

public class SqlServerDatabaseResource : ContainerResource, ISqlServerResource, IDistributedApplicationResourceWithParent<SqlServerContainerResource>
{
    public SqlServerDatabaseResource(string name, SqlServerContainerResource sqlServerContainer) : base(name)
    {
        Parent = sqlServerContainer;
    }

    public SqlServerContainerResource Parent { get; }

    public string? GetConnectionString(IDistributedApplicationResource? targetResource)
    {
        if (Parent.GetConnectionString(targetResource) is { } connectionString)
        {
            return $"{connectionString}Database={Name}";
        }
        else
        {
            throw new DistributedApplicationException("Parent resource connection string was null.");
        }
    }
}
