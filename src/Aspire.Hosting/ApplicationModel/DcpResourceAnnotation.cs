// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

public class DcpResourceAnnotation(string resourceNamespace, string resourceName, string resourceKind) : IResourceAnnotation
{
    public string ResourceNamespace { get; } = resourceNamespace ?? "";
    public string ResourceName { get; } = resourceName;
    public string ResourceKind { get; } = resourceKind;
}
