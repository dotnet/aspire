// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Azure;

/// <summary>
/// Wraps an <see cref="AzureServiceBusResource" /> in a type that exposes container extension methods.
/// </summary>
/// <param name="innerResource">The inner resource used to store annotations.</param>
public class AzureServiceBusEmulatorResource(AzureServiceBusResource innerResource) : ContainerResource(innerResource.Name), IResource
{
    // The path to the emulator configuration file in the container.
    internal const string EmulatorConfigJsonPath = "/ServiceBus_Emulator/ConfigFiles/Config.json";

    private readonly AzureServiceBusResource _innerResource = innerResource;

    /// <inheritdoc />
    public override ResourceAnnotationCollection Annotations => _innerResource.Annotations;
}
