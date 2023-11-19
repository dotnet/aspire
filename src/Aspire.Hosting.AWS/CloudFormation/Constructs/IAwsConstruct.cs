// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.AWS.CloudFormation.Constructs;

public interface IAwsConstruct
{
    string Name { get; }
    string Type { get; }
    Dictionary<string, string>? Tags { get; init; }
}
