// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Azurite;

public class AzuriteResource(string name, string? connectionString) : DistributedApplicationResource(name), IAzuriteResource
{
    public string? GetConnectionString() => connectionString;
}
