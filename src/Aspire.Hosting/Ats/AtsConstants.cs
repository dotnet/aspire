// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Ats;

/// <summary>
/// Constants for ATS (Aspire Type System) type IDs and capability IDs.
/// </summary>
public static class AtsConstants
{
    /// <summary>
    /// The Aspire.Hosting assembly name.
    /// </summary>
    public const string AspireHostingAssembly = "Aspire.Hosting";

    #region Primitive Type IDs

    /// <summary>
    /// String primitive type ID.
    /// </summary>
    public const string String = "string";

    /// <summary>
    /// Number primitive type ID.
    /// </summary>
    public const string Number = "number";

    /// <summary>
    /// Boolean primitive type ID.
    /// </summary>
    public const string Boolean = "boolean";

    /// <summary>
    /// Any type ID (unknown/dynamic type).
    /// </summary>
    public const string Any = "any";

    /// <summary>
    /// Void type ID (no return value).
    /// </summary>
    public const string Void = "void";

    #endregion

    #region Well-known Type IDs

    /// <summary>
    /// Type ID for IDistributedApplicationBuilder.
    /// </summary>
    public const string BuilderTypeId = "Aspire.Hosting/Aspire.Hosting.IDistributedApplicationBuilder";

    /// <summary>
    /// Type ID for DistributedApplication.
    /// </summary>
    public const string ApplicationTypeId = "Aspire.Hosting/Aspire.Hosting.DistributedApplication";

    /// <summary>
    /// Type ID for ReferenceExpression.
    /// </summary>
    public const string ReferenceExpressionTypeId = "Aspire.Hosting/Aspire.Hosting.ApplicationModel.ReferenceExpression";

    #endregion

    #region Well-known Capability IDs

    /// <summary>
    /// Capability ID for creating a builder.
    /// </summary>
    public const string CreateBuilderCapability = "Aspire.Hosting/createBuilder";

    /// <summary>
    /// Capability ID for building the application.
    /// </summary>
    public const string BuildCapability = "Aspire.Hosting/build";

    /// <summary>
    /// Capability ID for running the application.
    /// </summary>
    public const string RunCapability = "Aspire.Hosting/run";

    #endregion

    #region Collection Type ID Helpers

    /// <summary>
    /// Creates a List type ID for the given element type.
    /// </summary>
    /// <param name="elementType">The element type ID.</param>
    /// <returns>The List type ID.</returns>
    public static string ListTypeId(string elementType) => $"{AspireHostingAssembly}/List<{elementType}>";

    /// <summary>
    /// Creates a Dict type ID for the given key and value types.
    /// </summary>
    /// <param name="keyType">The key type ID.</param>
    /// <param name="valueType">The value type ID.</param>
    /// <returns>The Dict type ID.</returns>
    public static string DictTypeId(string keyType, string valueType) => $"{AspireHostingAssembly}/Dict<{keyType},{valueType}>";

    #endregion
}
