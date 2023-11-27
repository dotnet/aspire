// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace Aspire.Hosting.AWS.CloudFormation.Functions;

/// <summary>
/// Represents the 'Fn::GetAtt' intrinsic function in CloudFormation,
/// used to obtain the value of an attribute from another resource in the template.
/// </summary>
/// <param name="resourceName">The logical name of the resource whose attribute is being retrieved.</param>
/// <param name="attributeName">The name of the attribute whose value is being retrieved.</param>
internal sealed class FnGetAtt(string resourceName, string attributeName)
{
    [JsonPropertyName("Fn::GetAtt")]
    public List<string> Arguments { get; set; } = [resourceName, attributeName];
}
