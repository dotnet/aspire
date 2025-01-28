// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Azure;

/// <summary>
/// Wraps an <see cref="AzureEventHubsResource" /> in a type that exposes container extension methods.
/// </summary>
/// <param name="innerResource">The inner resource used to store annotations.</param>
public class AzureEventHubsEmulatorResource(AzureEventHubsResource innerResource) : ContainerResource(innerResource.Name, innerResource.Annotations), IResource
{
    // The path to the emulator configuration file in the container.
    internal const string EmulatorConfigJsonPath = "/Eventhubs_Emulator/ConfigFiles/Config.json";

    /// <inheritdoc/>
    public override string Name => base.Name;

    /// <inheritdoc />
    public override ResourceAnnotationCollection Annotations => base.Annotations;
}
