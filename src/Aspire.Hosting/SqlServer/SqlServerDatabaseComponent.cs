// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.SqlServer;

public class SqlServerDatabaseComponent : ContainerComponent, ISqlServerComponent, IDistributedApplicationComponentWithParent<SqlServerContainerComponent>
{
    public SqlServerDatabaseComponent(string name, SqlServerContainerComponent sqlServerContainer) : base(name)
    {
        Parent = sqlServerContainer;
    }

    public SqlServerContainerComponent Parent { get; }

    public string? GetConnectionString()
    {
        if (Parent.GetConnectionString() is { } connectionString)
        {
            return $"{connectionString}Database={Name}";
        }
        else
        {
            throw new DistributedApplicationException("Parent component connection string was null.");
        }
    }
}
