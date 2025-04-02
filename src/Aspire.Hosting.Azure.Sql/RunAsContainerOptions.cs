// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Azure.Sql;

/// <summary>
/// Options for configuring the behavior of <see cref="AzureSqlExtensions.RunAsContainer(IResourceBuilder{AzureSqlServerResource}, Action{IResourceBuilder{SqlServerServerResource}}?, Action{RunAsContainerOptions}?)"/>.
/// </summary>
public sealed class RunAsContainerOptions
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
