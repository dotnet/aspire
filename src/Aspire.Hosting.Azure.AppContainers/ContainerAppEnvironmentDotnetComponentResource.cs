// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Azure.Provisioning.Primitives;

namespace Azure.Provisioning.AppContainers;

// This is missing from Azure.Provisioning.AppContainers
internal sealed class ContainerAppEnvironmentDotnetComponentResource(string bicepIdentifier, string? resourceVersion = null) : ProvisionableResource(bicepIdentifier, new("Microsoft.App/managedEnvironments/dotNetComponents"), resourceVersion)
{
    public BicepValue<string> Name
    {
        get { Initialize(); return _name!; }
        set { Initialize(); _name!.Assign(value); }
    }
    private BicepValue<string>? _name;

    public BicepValue<string> ComponentType
    {
        get { Initialize(); return _componentType!; }
        set { Initialize(); _componentType!.Assign(value); }
    }
    private BicepValue<string>? _componentType;

    public ContainerAppManagedEnvironment? Parent
    {
        get { Initialize(); return _parent!.Value; }
        set { Initialize(); _parent!.Value = value; }
    }

    private ResourceReference<ContainerAppManagedEnvironment>? _parent;

    protected override void DefineProvisionableProperties()
    {
        _name = DefineProperty<string>(nameof(Name), ["name"], isOutput: false, isRequired: true);
        _componentType = DefineProperty<string>(nameof(ComponentType), ["properties", "componentType"], isOutput: false, isRequired: true);
        _parent = DefineResource<ContainerAppManagedEnvironment>(nameof(Parent), ["parent"], isRequired: true);
    }
}
