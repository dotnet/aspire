// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Dapr;

/// <summary>
/// Options for configuring a Dapr component.
/// </summary>
public sealed record DaprComponentOptions
{
    /// <summary>
    /// Gets or sets the path to the component configuration file.
    /// </summary>
    /// <remarks>
    /// If specified, the folder containing the configuration file will be added to all associated Dapr sidecars' resources paths.
    /// </remarks>
    public string? LocalPath { get; init; }
    // /// <summary>
    // /// Metadata for the component
    // /// </summary>
    // public List<MetadataOptionsValue> Metadata { get; init; } = new List<MetadataOptionsValue>();
    // /// <summary>
    // /// The optional secret store ref.
    // /// Is required if the <see cref="Metadata"/> contains a reference to a secret.
    // /// </summary>
    // public DaprComponentResource? SecretStore { get; init; }
}

// /// <summary>
// /// A single metadata value for dapr
// /// </summary>
// /// <param name="Name">The name of the metadata options</param>
// /// <param name="Value">The name of the metadata options</param>
// public abstract class MetadataOptionsValue(string Name)
// {
//     /// <summary>
//     /// Name of the metadata value
//     /// </summary>
//     public string Name { get; } = Name;
//     /// <summary>
//     /// Value of the metadata value
//     /// </summary>
//     public abstract string Value();

//     /// <summary>
//     /// The yaml representation of the metadata value
//     /// </summary>
//     public override abstract string ToString();
// }

// /// <inheritdoc/>
// public class SecretOptionsValue : MetadataOptionsValue
// {
//     private readonly string _secretName;
//     private readonly string _secretKey;
//     /// <inheritdoc/>
//     public SecretOptionsValue(string Name, string SecretName, string SecretKey) : base(Name)
//     {
//         _secretName = SecretName;
//         _secretKey = SecretKey;
//     }

//     private static string SecretRef(string SecretName, string SecretKey)
//     {
//         return $"""
//   secretKeyRef:
//     name: {SecretName}
//     key: {SecretKey}
// """;
//     }

//     /// <inheritdoc/>
//     public override string ToString()
//     {
//         return $"""
// - name: {base.Name}
// {Value()}
// """;
//     }
//     /// <inheritdoc/>
//     public override string Value() => SecretRef(_secretName, _secretKey);
// }

// /// <summary>
// /// Representation of a value in a
// /// </summary>
// public class ValueOptionsValue : MetadataOptionsValue
// {
//     private readonly string _value;
//     /// <inheritdoc/>
//     public ValueOptionsValue(string Name, string Value) : base(Name)
//     {
//         _value = $@"""{Value}""";
//     }

//     /// <inheritdoc/>
//     public ValueOptionsValue(string Name, bool Value) : base(Name, $@"{Value}")
//     {
//         _value = $@"{Value}";
//     }

//     /// <inheritdoc/>
//     public ValueOptionsValue(string Name, long Value) : base(Name, $@"{Value}")
//     {
//         _value = $@"{Value}";
//     }

//     /// <inheritdoc/>
//     public override string ToString()
//     {
//         return $"""
// - name: {base.Name}
//   value: {Value()}
// """;
//     }

//     public override string Value() => _value;
// }

// public class ResourceSecretOptionsValue<TResource> : MetadataOptionsValue
//     where TResource : Resource, IResourceWithConnectionString
// {
//     private readonly TResource _value;

//     public ResourceSecretOptionsValue(string Name, TResource Value) : base(Name)
//     {
//         _value = Value;
//     }

//     public override string ToString()
//     {
//         throw new NotImplementedException();
//     }

//     public override string Value()
//     {
//         throw new NotImplementedException();
//     }
// }