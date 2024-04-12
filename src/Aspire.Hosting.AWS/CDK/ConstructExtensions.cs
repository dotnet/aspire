// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Amazon.CDK;
using Constructs;

namespace Aspire.Hosting.AWS.CDK;

internal static class ConstructExtensions
{
    public static string StackUniqueId(this IConstruct construct)
    {
        var stack = construct.Node.Scopes.OfType<Stack>().FirstOrDefault();
        return stack is null ? Names.UniqueId(construct) : Names.UniqueId(construct).TrimStart(Names.UniqueId(stack));
    }
}
