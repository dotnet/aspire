// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Amazon.CDK.CXAPI;
using Aspire.Hosting.AWS.CloudFormation;

namespace Aspire.Hosting.AWS.CDK;

internal sealed class CDKStackTemplate(CloudFormationStackArtifact artifact, IStackResource resource) : ICloudFormationTemplateProvider
{
    public IStackResource Resource { get; } = resource;
    public CloudFormationStackArtifact Artifact { get; } = artifact;
    public string StackName { get; } = artifact.StackName;
    public IDictionary<string, string> CloudFormationParameters { get; } = new Dictionary<string, string>();
    public string? RoleArn { get; set; }
    public int StackPollingInterval { get; set; }
    public bool DisableDiffCheck { get; set; }
    public IList<string> DisabledCapabilities { get; } = [];
    public Task<string> GetCloudFormationTemplate(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(JsonSerializer.Serialize(Artifact.Template));
    }
}
