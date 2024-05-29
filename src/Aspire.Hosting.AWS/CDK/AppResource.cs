// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Amazon.CDK;
using Constructs;
using Resource = Aspire.Hosting.ApplicationModel.Resource;

namespace Aspire.Hosting.AWS.CDK;

/// <inheritdoc cref="Aspire.Hosting.AWS.CDK.IAppResource" />
internal class AppResource(string name, IAppProps? props = default) : Resource(name), IAppResource
{
    public App App { get; } = new(props);

    IConstruct IResourceWithConstruct.Construct => App;
}
