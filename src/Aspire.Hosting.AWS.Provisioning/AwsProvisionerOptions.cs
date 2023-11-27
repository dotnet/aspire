// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Amazon.Extensions.NETCore.Setup;

namespace Aspire.Hosting.AWS.Provisioning;

// TODO: Re-evaluate this approach
internal sealed class AwsProvisionerOptions : AWSOptions
{
    public string? StackName { get; set; }
}
