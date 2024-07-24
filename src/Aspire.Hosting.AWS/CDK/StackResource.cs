// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Amazon.CDK;
using Aspire.Hosting.AWS.CloudFormation;
using Constructs;
using Stack = Amazon.CDK.Stack;

namespace Aspire.Hosting.AWS.CDK;

/// <inheritdoc cref="Aspire.Hosting.AWS.CDK.IStackResource" />
internal class StackResource(string name, Stack stack) : CloudFormationTemplateResource(name, stack.StackName, stack.GetTemplatePath()), IStackResource
{
    /// <inheritdoc/>
    public Stack Stack { get; } = stack;

    /// <inheritdoc/>
    public IConstruct Construct => Stack;

    /// <summary>
    /// The AWS CDK App the stack belongs to. This is needed for building the AWS CDK app tree.
    /// </summary>
    public App App => (App)Stack.Node.Root;
}

/// <inheritdoc cref="Aspire.Hosting.AWS.CDK.StackResource" />
internal sealed class StackResource<T>(string name, T stack) : StackResource(name, stack), IStackResource<T>
    where T : Stack
{
    public new T Stack { get; } = stack;
    public new T Construct => Stack;
}
