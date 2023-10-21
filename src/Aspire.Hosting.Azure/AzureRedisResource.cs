// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Azure;

public class AzureRedisResource(string name) : DistributedApplicationResource(name), IAzureResource, IDistributedApplicationResourceWithConnectionString
{
    public string? ConnectionString { get; set; }

    public string? GetConnectionString() => ConnectionString;
}
