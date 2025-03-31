// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Azure.Sql;

/// <summary>  
/// Represents options for running a SQL Server as a container.  
/// This class allows configuration of the SQL Server container,   
/// including the port to expose and the password for the SQL Server instance.  
/// </summary>  
public class RunAsContainerOptions
{
    /// <summary>  
    /// Gets or Sets the port for the SQL Server.  
    /// </summary>  
    public int? Port { get; set; }

    /// <summary>  
    /// Gets or Sets the password for the SQL Server.  
    /// </summary>  
    public IResourceBuilder<ParameterResource>? Password { get; set; }
}
