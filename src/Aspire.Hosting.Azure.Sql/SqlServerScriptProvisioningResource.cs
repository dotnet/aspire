// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Azure.Provisioning;
using Azure.Provisioning.AppContainers;
using Azure.Provisioning.Resources;

namespace Aspire.Hosting.Azure;

sealed class SqlServerScriptProvisioningResource : ArmDeploymentScript
{
    private BicepValue<string>? _scriptContent;
    private BicepValue<string>? _kind;
    private BicepValue<string>? _azCliVersion;
    private BicepValue<TimeSpan>? _retentionInterval;
    private BicepList<ContainerAppEnvironmentVariable>? _environmentVariables;

    public SqlServerScriptProvisioningResource(string bicepIdentifier) : base(bicepIdentifier)
    {
        Kind = "AzureCLI";
        AZCliVersion = "2.59.0";
        RetentionInterval = TimeSpan.FromHours(1);
    }

    /// <summary>
    /// Type of the script. e.g., AzurePowerShell, .
    /// </summary>
    public BicepValue<string> Kind
    {
        get { Initialize(); return _kind!; }
        set { Initialize(); _kind!.Assign(value); }
    }

    /// <summary>
    /// Script body.
    /// </summary>
    public BicepValue<string> ScriptContent
    {
        get { Initialize(); return _scriptContent!; }
        set { Initialize(); _scriptContent!.Assign(value); }
    }

    /// <summary>
    /// Azure CLI module version to be used.
    /// </summary>
    public BicepValue<string> AZCliVersion
    {
        get { Initialize(); return _azCliVersion!; }
        set { Initialize(); _azCliVersion!.Assign(value); }
    }

    /// <summary>
    /// Interval for which the service retains the script resource after it reaches a terminal state. Resource will be deleted when this duration expires. Duration is based on ISO 8601 pattern (for example P1D means one day).
    /// </summary>
    public BicepValue<TimeSpan> RetentionInterval
    {
        get { Initialize(); return _retentionInterval!; }
        set { Initialize(); _retentionInterval!.Assign(value); }
    }

    public BicepList<ContainerAppEnvironmentVariable> EnvironmentVariables
    {
        get { Initialize(); return _environmentVariables!; }
        set { Initialize(); _environmentVariables!.Assign(value); }
    }

    protected override void DefineProvisionableProperties()
    {
        base.DefineProvisionableProperties();

        _kind = DefineProperty<string>(nameof(Kind), ["kind"], isRequired: true);
        _scriptContent = DefineProperty<string>(nameof(ScriptContent), ["properties", "scriptContent"]);
        _azCliVersion = DefineProperty<string>(nameof(AZCliVersion), ["properties", "azCliVersion"]);
        _retentionInterval = DefineProperty<TimeSpan>(nameof(RetentionInterval), ["properties", "retentionInterval"], format: "P");
        _environmentVariables = DefineListProperty<ContainerAppEnvironmentVariable>(nameof(EnvironmentVariables), ["properties", "environmentVariables"]);
    }
}
