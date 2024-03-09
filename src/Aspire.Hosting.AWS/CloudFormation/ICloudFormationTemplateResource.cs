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

    /// <summary>
    /// Add parameters to be provided to CloudFormation when creating the stack for the template.
    /// </summary>
    /// <param name="parameterName">Name of the CloudFormation parameter.</param>
    /// <param name="parameterValue">Value of the CloudFormation parameter.</param>
    /// <returns></returns>
    ICloudFormationTemplateResource AddParameter(string parameterName, string parameterValue);

    /// <summary>
    /// The optional IAM role assumed by CloudFormation when creating or updating the Stack.
    /// </summary>
    string? RoleArn { get; set; }

    /// <summary>
    /// The interval in seconds to poll CloudFormation for changes. The default is 3 seconds.
    /// This value can be increased to avoid throttling errors polling CloudFormation.
    /// </summary>
    int StackPollingInterval { get; set; }

    /// <summary>
    /// By default provisioning checks if the template and parameters haven't changed since the
    /// last provisioning. If they haven't changed then provisioning is skipped.
    /// Setting this property to true disables the check and always attempt provisioning.
    /// </summary>
    bool DisableDiffCheck { get; set; }

    /// <summary>
    /// By default CloudFormation provisioning is given the CAPABILITY_IAM, CAPABILITY_NAMED_IAM
    /// and CAPABILITY_AUTO_EXPAND capabilities. To disable any or all of these capabilities add
    /// the these names to this collection.
    /// </summary>
    IList<string> DisabledCapabilities { get; }
}
