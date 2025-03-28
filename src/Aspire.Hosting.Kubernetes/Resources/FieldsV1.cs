// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Serialization;

namespace Aspire.Hosting.Kubernetes.Resources;

/// <summary>
/// Represents the FieldsV1 class as part of the Kubernetes resource management utilities.
/// </summary>
/// <remarks>
/// FieldsV1 is a sealed class intended to define a field structure for use within
/// Kubernetes managed objects. It provides the ability to describe serialized field data.
/// This class is utilized as a component in conjunction with ManagedFieldsEntryV1.
/// </remarks>
[YamlSerializable]
public sealed class FieldsV1;
