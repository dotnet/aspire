// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace Aspire.Hosting.AWS.CloudFormation.Constructs;

/// <summary>
/// Serves as the base class for AWS resource types to be included in a CloudFormation template.
/// </summary>
/// <param name="name">The name of the resource.</param>
internal abstract class AwsConstruct(string name)
{
    [JsonIgnore]
    public string Name { get; } = name;

    public abstract string Type { get;  }

    public Dictionary<string, string>? Tags { get; init; }

    public abstract class Properties
    {
    }
}
