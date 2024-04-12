// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.AWS.CloudFormation;
using Constructs;
using Stack = Amazon.CDK.Stack;

namespace Aspire.Hosting.AWS.CDK;

internal class StackResource(string name, Stack stack) : CloudFormationResource(name), IStackResource
{
    public Stack Stack { get; } = stack;

    public IConstruct Construct => Stack;
}

internal sealed class StackResource<T>(string name, T stack) : StackResource(name, stack), IStackResource<T>
    where T : Stack
{
    public new T Stack { get; } = stack;
    public new T Construct => Stack;
}
