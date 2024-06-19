// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.AWS.CDK;

namespace Aspire.Hosting.AWS.Provisioning;

internal sealed class CDKResourceProvisioner : AWSResourceProvisioner<CDKResource>
{
    protected override Task GetOrCreateResourceAsync(CDKResource resource, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
