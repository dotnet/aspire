// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.AWS.CloudFormation;
using Constructs;
using Stack = Amazon.CDK.Stack;

namespace Aspire.Hosting.AWS.CDK;

internal class StackResource(string name, Stack stack, IAppResource appResource) : CloudFormationResource(name), IStackResource
{
    public Stack Stack { get; } = stack;

    public IAppResource Parent { get; } = appResource;

    public Construct Construct => Stack;
}

internal sealed class StackResource<T>(string name, T stack, IAppResource appResource) : StackResource(name, stack, appResource), IStackResource<T>
    where T : Stack
{
    public new T Stack { get; } = stack;
    public new T Construct => Stack;
}
