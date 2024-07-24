// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Amazon.CDK;
using Constructs;

namespace Aspire.Hosting.AWS.CDK;

internal sealed class ConstructOutputAnnotation<T>(string name, ConstructOutputDelegate<T> output)
    : IConstructModifierAnnotation, IConstructOutputAnnotation
    where T : IConstruct
{
    public string OutputName { get; } = name;

    /// <inheritdoc cref="IConstructModifierAnnotation"/>
    public void ChangeConstruct(IConstruct construct)
    {
        // Find the stack where this construct belongs to.
        if (construct is not Stack stack)
        {
            stack = construct.Node.Scopes.OfType<Stack>().FirstOrDefault() ?? throw new InvalidOperationException("Construct is not part of a Stack");
        }

        // Add a CloudFormation output on the stack referencing the construct and the resolved value.
        _ = new CfnOutput(stack, OutputName, new CfnOutputProps
        {
            Key = $"{construct.GetStackUniqueId()}{OutputName}",
            Value = output((T)construct)
        });
    }
}
