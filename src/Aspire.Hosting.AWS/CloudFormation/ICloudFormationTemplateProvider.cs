// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.AWS.CloudFormation;

/// <summary>
///
/// </summary>
public interface ICloudFormationTemplateProvider
{
    /// <summary>
    ///
    /// </summary>
    string StackName { get; }

    /// <summary>
    ///
    /// </summary>
    IDictionary<string, string> CloudFormationParameters { get; }

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
    /// By default, provisioning checks if the template and parameters haven't changed since the
    /// last provisioning. If they haven't changed then provisioning is skipped.
    /// Setting this property to true disables the check and always attempt provisioning.
    /// </summary>
    bool DisableDiffCheck { get; set; }

    /// <summary>
    /// By default, CloudFormation provisioning is given the CAPABILITY_IAM, CAPABILITY_NAMED_IAM
    /// and CAPABILITY_AUTO_EXPAND capabilities. To disable any or all of these capabilities add
    /// the these names to this collection.
    /// </summary>
    IList<string> DisabledCapabilities { get; }

    /// <summary>
    ///
    /// </summary>
    /// <returns></returns>
    Task<string> GetCloudFormationTemplate(CancellationToken cancellationToken = default);
}
