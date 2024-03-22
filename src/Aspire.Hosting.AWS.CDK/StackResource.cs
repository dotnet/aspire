// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Amazon.CDK;
using Aspire.Hosting.AWS.CloudFormation;

namespace Aspire.Hosting.AWS.CDK;

internal abstract class StackResource(string name) : CloudFormationResource(name), IStackResource
{
    public abstract Stack BuildStack(App app);
}

internal sealed class StackResource<T>(string name, StackBuilderDelegate<T> stackBuilder) : StackResource(name), IStackResource<T>
    where T : Stack
{
    public StackBuilderDelegate<T> StackBuilder { get; } = stackBuilder;

    public override Stack BuildStack(App app) => StackBuilder(app);
}
