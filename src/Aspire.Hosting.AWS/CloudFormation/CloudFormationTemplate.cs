// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.AWS.CloudFormation.Constructs;

namespace Aspire.Hosting.AWS.CloudFormation;

/// <summary>
/// Represents an AWS CloudFormation template, encapsulating the format version, a description,
/// and a collection of resources to be deployed.
/// </summary>
internal sealed class CloudFormationTemplate
{
    private readonly Dictionary<string, AwsConstruct> _resources = new();

    public string AWSTemplateFormatVersion { get; init; } = "2010-09-09";
    public string? Description { get; init; }

    public IReadOnlyDictionary<string, AwsConstruct> Resources => _resources;

    /// <summary>
    /// Adds an AWS resource to the CloudFormation template.
    /// </summary>
    /// <param name="construct">The AWS resource to add.</param>
    /// <exception cref="Exception">Thrown when a resource with the same name already exists.</exception>
    public void AddResource(AwsConstruct construct)
    {
        // TODO: Maybe some validation here?

        if (!_resources.TryAdd(construct.Name, construct))
        {
            throw new Exception($"Resource with name {construct.Name} already exists");
        }
    }
}
