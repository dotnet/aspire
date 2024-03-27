// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Amazon.CDK;
using Constructs;

namespace Aspire.Hosting.AWS.CDK;

internal sealed class ConstructOutputAnnotation<T>(string name, ConstructOutputDelegate<T> output)
    : IConstructModifierAnnotation
    where T : Construct
{
    public string Name { get; } = name;

    public string? ExportName { get; set; }

    public string? Description { get; set; }

    public void ChangeConstruct(Construct construct)
    {
        var target = (T)construct;
        _ = new CfnOutput(construct, Name, new CfnOutputProps
        {
            Value = output(target),
            Description = Description,
            ExportName = ExportName ?? $"{construct.Node.Id}::{Name}"
        });
    }
}
