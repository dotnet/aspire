// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.AWS.CloudFormation;

/// <summary>
/// Resource representing an AWS CloudFormation stack.
/// </summary>
public interface ICloudFormationTemplateResource : ICloudFormationResource
{
    /// <summary>
    /// Path to the CloudFormation template.
    /// </summary>
    string TemplatePath { get; }
}
