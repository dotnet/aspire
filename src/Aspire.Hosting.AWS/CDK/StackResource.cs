// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.AWS.CloudFormation;
using Constructs;
using Stack = Amazon.CDK.Stack;

namespace Aspire.Hosting.AWS.CDK;

internal class StackResource(string name, Stack stack, IResourceWithConstruct parentConstruct) : CloudFormationResource(name), IStackResource, IResourceWithParent<IResourceWithConstruct>
{
    public Stack Stack { get; } = stack;

    public string StackName => Stack.StackName;

    public IConstruct Construct => Stack;

    public IResourceWithConstruct Parent { get; } = parentConstruct;

    private IAWSSDKConfig? _awsSdkConfig;
    IAWSSDKConfig? IAWSResource.AWSSDKConfig
    {
        get => _awsSdkConfig ?? this.FindParentOfType<ICloudFormationResource>().AWSSDKConfig;
        set => _awsSdkConfig = value;
    }

    protected override string GetStackName() => StackName;
}

internal sealed class StackResource<T>(string name, T stack, IResourceWithConstruct parentConstruct) : StackResource(name, stack, parentConstruct), IStackResource<T>
    where T : Stack
{
    public new T Stack { get; } = stack;
    public new T Construct => Stack;
}
