// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Azure.ResourceManager.Resources;

namespace Aspire.Hosting.Azure.Provisioning.Internal;

/// <summary>
/// Default implementation of <see cref="ITenantData"/>.
/// </summary>
internal sealed class DefaultTenantData(TenantData tenantData) : ITenantData
{
    public Guid? TenantId => tenantData.TenantId;
    public string? DefaultDomain => tenantData.DefaultDomain;
}