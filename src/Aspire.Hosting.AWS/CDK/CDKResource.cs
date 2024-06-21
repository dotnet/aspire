// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Amazon.CDK;
using Aspire.Hosting.AWS.CloudFormation;
using Constructs;

namespace Aspire.Hosting.AWS.CDK;

internal class CDKResource : CloudFormationResource, ICDKResource
{
    public CDKResource(string name, string? stackName = null, IAppProps? props = default, IStackProps? stackProps = default)
        : base(name)
    {
        // Use the name with 'Aspire-' prefix when stackName is not provided
        stackName ??= "Aspire-" + name;

        App = new App(props);
        Stack = new Stack(App, stackName, stackProps);
    }

    public App App { get; }

    public Stack Stack { get; }

    public string StackName => Stack.StackName;

    protected override string GetStackName() => StackName;

    IConstruct IResourceWithConstruct.Construct => Stack;
}
