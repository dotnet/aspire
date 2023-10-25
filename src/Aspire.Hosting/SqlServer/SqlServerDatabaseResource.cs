// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

public class SqlServerDatabaseResource(string name, SqlServerContainerResource sqlServerContainer) : ContainerResource(name), ISqlServerResource, IResourceWithParent<SqlServerContainerResource>
{
    public SqlServerContainerResource Parent { get; } = sqlServerContainer;

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
