// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Amazon.CDK.CXAPI;
using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.AWS.CDK;

/// <summary>
/// Annotations that records the template path for a CloudFormation resources.
/// </summary>
/// <param name="stackArtifact"></param>
[DebuggerDisplay("Type = {GetType().Name,nq}, StackName = {StackArtifact.StackName}")]
internal sealed class StackArtifactResourceAnnotation(CloudFormationStackArtifact stackArtifact) : IResourceAnnotation
{
   
    public CloudFormationStackArtifact StackArtifact { get; } = stackArtifact;
}
