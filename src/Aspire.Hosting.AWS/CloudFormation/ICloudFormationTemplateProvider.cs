// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.AWS.CloudFormation;

internal interface ICloudFormationTemplateProvider
{
    string StackName { get; }

    IDictionary<string, string> CloudFormationParameters { get; }

    string? RoleArn { get; set; }

    int StackPollingInterval { get; set; }

    bool DisableDiffCheck { get; set; }

    IList<string> DisabledCapabilities { get; }

    Task<string> GetCloudFormationTemplate(CancellationToken cancellationToken = default);
}
