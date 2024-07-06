// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Amazon.CDK;
using Constructs;

namespace Aspire.Hosting.AWS.CDK;

internal static class ConstructExtensions
{
    /// <summary>
    /// Returns the computed unique ID of a construct in the stack.
    /// </summary>
    /// <param name="construct">The construct in the stack</param>
    public static string GetStackUniqueId(this IConstruct construct)
    {
        var stack = construct.GetStack();
        return stack is null ? Names.UniqueId(construct) : Names.UniqueId(construct).TrimStart(Names.UniqueId(stack));
    }

    /// <summary>
    /// Returns the stack of the construct.
    /// </summary>
    /// <param name="construct">The construct in the stack</param>
    private static Stack? GetStack(this IConstruct construct)
        => construct.Node.Scopes.OfType<Stack>().FirstOrDefault();

    /// <summary>
    /// Returns the path of the CloudFormation template file for this stack.
    /// </summary>
    /// <param name="stack">The stack</param>
    /// <returns>The path of the CloudFormation template file for this stack</returns>
    public static string GetTemplatePath(this Stack stack)
    {
        return Path.Combine(((App)stack.Node.Root).Outdir, $"{stack.StackName}.template.json");
    }
}
