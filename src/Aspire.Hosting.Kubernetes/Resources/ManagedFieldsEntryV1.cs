// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Serialization;

namespace Aspire.Hosting.Kubernetes.Resources;

/// <summary>
/// Represents an entry detailing managed fields within a Kubernetes resource metadata.
/// </summary>
/// <remarks>
/// This class provides information about the field-level changes made to a Kubernetes object.
/// It includes details such as the fields affected, the manager responsible for the changes,
/// the operation performed, and the API version used during the modification.
/// </remarks>
[YamlSerializable]
public sealed class ManagedFieldsEntryV1
{
    /// <summary>
    /// Represents a structure used for describing serialized field data in Kubernetes resources.
    /// </summary>
    /// <remarks>
    /// This class is part of the Kubernetes resource management utilities and allows
    /// for defining field-level information associated with Kubernetes objects. It is
    /// generally used in conjunction with <see cref="ManagedFieldsEntryV1"/> to provide
    /// detailed information about managed fields within a Kubernetes resource object.
    /// </remarks>
    [YamlMember(Alias = "fieldsV1")]
    public FieldsV1 FieldsV1 { get; set; } = new();

    /// <summary>
    /// Represents the type of managed fields for a Kubernetes resource.
    /// </summary>
    /// <remarks>
    /// The FieldsType property identifies the type of field management information
    /// associated with a Kubernetes resource entry. It is part of the serialization
    /// structure for managed fields in Kubernetes.
    /// </remarks>
    [YamlMember(Alias = "fieldsType")]
    public string FieldsType { get; set; } = null!;

    /// <summary>
    /// Gets or sets the subresource associated with the managed fields entry.
    /// </summary>
    /// <remarks>
    /// This property specifies the subresource of the Kubernetes object that the managed fields
    /// are applied to. It is typically used to provide context about the specific subresource
    /// being accessed or managed.
    /// </remarks>
    [YamlMember(Alias = "subresource")]
    public string Subresource { get; set; } = null!;

    /// <summary>
    /// Gets or sets the timestamp indicating the time of the operation described in the managed fields entry.
    /// </summary>
    /// <remarks>
    /// Represents the date and time of the action associated with this entry in the managed fields.
    /// The value is nullable, meaning it can be null if the time is not specified or unavailable.
    /// </remarks>
    [YamlMember(Alias = "time")]
    public DateTime? Time { get; set; }

    /// <summary>
    /// Gets or sets the API version of the resource being managed.
    /// </summary>
    /// <remarks>
    /// This property represents the API version used in the managed fields entry.
    /// It is a critical component for versioning resources in Kubernetes and
    /// ensures compatibility with the defined API specifications.
    /// </remarks>
    [YamlMember(Alias = "apiVersion")]
    public string ApiVersion { get; set; } = null!;

    /// <summary>
    /// Represents the operation type performed on the Kubernetes resource.
    /// </summary>
    /// <remarks>
    /// The Operation property specifies the type of operation (e.g., Apply, Update, etc.)
    /// that was executed on the managed resource. It is used to track the state changes
    /// performed by various controllers or users in the Kubernetes system.
    /// </remarks>
    [YamlMember(Alias = "operation")]
    public string Operation { get; set; } = null!;

    /// <summary>
    /// Gets or sets the name of the entity, application, or process that is managing the resource.
    /// </summary>
    /// <remarks>
    /// The Manager property identifies the controller or user responsible for making updates or changes
    /// to the managed fields. This information is typically used for auditing and tracking purposes
    /// within Kubernetes-managed resources.
    /// </remarks>
    [YamlMember(Alias = "manager")]
    public string Manager { get; set; } = null!;
}
