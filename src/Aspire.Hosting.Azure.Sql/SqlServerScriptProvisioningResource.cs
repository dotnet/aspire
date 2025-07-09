// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Azure.Provisioning;
using Azure.Provisioning.Resources;

namespace Aspire.Hosting.Azure;

// The AzurePowerShellScript class doesn't work correctly.
// See https://github.com/Azure/azure-sdk-for-net/issues/51135
// Reference: https://learn.microsoft.com/azure/azure-resource-manager/templates/deployment-script-template
sealed class SqlServerScriptProvisioningResource : AzurePowerShellScript
{
    private BicepValue<TimeSpan>? _retentionIntervalOverride;

    public SqlServerScriptProvisioningResource(string bicepIdentifier) : base(bicepIdentifier)
    {
        RetentionInterval = TimeSpan.FromHours(1);
        RetentionIntervalOverride = TimeSpan.FromHours(1);
    }

    /// <summary>
    /// Interval for which the service retains the script resource after it reaches a terminal state. Resource will be deleted when this duration expires. Duration is based on ISO 8601 pattern (for example P1D means one day).
    /// </summary>
    public BicepValue<TimeSpan> RetentionIntervalOverride
    {
        get { Initialize(); return _retentionIntervalOverride!; }
        set { Initialize(); _retentionIntervalOverride!.Assign(value); }
    }

    protected override void DefineProvisionableProperties()
    {
        base.DefineProvisionableProperties();

        DefineProperty<string>("Kind", ["kind"], defaultValue: "AzurePowerShell");
        _retentionIntervalOverride = DefineProperty<TimeSpan>(nameof(RetentionInterval), ["properties", "retentionInterval"], format: "P");
    }
}
