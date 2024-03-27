// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Amazon.CDK;
using Constructs;
using Resource = Aspire.Hosting.ApplicationModel.Resource;

namespace Aspire.Hosting.AWS.CDK;

internal sealed class AppResource() : Resource("AWSCDK"), IAppResource
{
    public App App { get; } = new();

    public Construct Construct => App;
}
