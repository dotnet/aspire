// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Azure.Data.Cosmos;

public class CosmosDBConnectionResource(string name, string? connectionString)
    : Resource(name), IResourceWithConnectionString
{
    public string? GetConnectionString() => connectionString;
}
