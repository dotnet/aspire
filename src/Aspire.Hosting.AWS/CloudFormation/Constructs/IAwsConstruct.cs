// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace Aspire.Hosting.AWS.CloudFormation.Constructs;

[JsonDerivedType(typeof(AwsS3BucketConstruct))]
[JsonDerivedType(typeof(AwsSnsTopicConstruct))]
[JsonDerivedType(typeof(AwsSqsQueueConstruct))]
public interface IAwsConstruct
{
    [JsonIgnore] string Name { get; }

    string Type { get; }

    Dictionary<string, string>? Tags { get; init; }

    IReadOnlyDictionary<string, CloudFormationTemplate.Output> GetOutputs();
}
