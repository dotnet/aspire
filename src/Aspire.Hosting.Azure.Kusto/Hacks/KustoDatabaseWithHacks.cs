// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable AZPROVISION001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

// Temporary hack until fixes merged to Azure.Provisioning.Kusto.
using Azure.Provisioning;
using Azure.Provisioning.Kusto;

internal class KustoDatabaseWithHacks(string bicepIdentifier, string? resourceVersion = default) : KustoDatabase(bicepIdentifier, resourceVersion)
{
    public BicepValue<string> Kind
    {
        get { Initialize(); return _kind!; }
        set { Initialize(); _kind!.Assign(value); }
    }
    private BicepValue<string>? _kind;

    protected override void DefineProvisionableProperties()
    {
        base.DefineProvisionableProperties();
        _kind = DefineProperty<string>(nameof(Kind), ["kind"], isRequired: true);
    }
}