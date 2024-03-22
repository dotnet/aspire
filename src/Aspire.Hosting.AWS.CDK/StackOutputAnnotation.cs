// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Amazon.CDK;

namespace Aspire.Hosting.AWS.CDK;

internal sealed class StackOutputAnnotation<T>(string name, StackOutputDelegate<T> output)
    : IStackModifierAnnotation
    where T : Stack
{
    public string Name { get; } = name;

    public string? Description { get; set; }

    public void ChangeStack(Stack stack)
    {
        var target = (T)stack;
        _ = new CfnOutput(stack, Name, new CfnOutputProps
        {
            Value = output(target),
            Description = Description
        });
    }
}
