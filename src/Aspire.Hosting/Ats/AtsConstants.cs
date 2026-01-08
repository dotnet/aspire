// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Ats;

/// <summary>
/// Categories of ATS types for serialization and handling.
/// </summary>
public enum AtsTypeCategory
{
    /// <summary>
    /// Primitive types that serialize directly to JSON values.
    /// Examples: string, number, boolean, datetime, guid.
    /// </summary>
    Primitive,

    /// <summary>
    /// Handle types that are opaque references to .NET objects.
    /// Serialized as { "$handle": "type:id", "$type": "type" }.
    /// </summary>
    Handle,

    /// <summary>
    /// Data Transfer Objects that serialize as JSON objects.
    /// Must be marked with [AspireDto].
    /// </summary>
    Dto,

    /// <summary>
    /// Callback types (delegates) that are registered and invoked by ID.
    /// </summary>
    Callback
}

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
    /// String type ID. Maps from .NET <see cref="System.String"/>.
    /// </summary>
    public const string String = "string";

    /// <summary>
    /// Char type ID. Maps from .NET <see cref="System.Char"/>.
    /// Serializes to JSON string.
    /// </summary>
    public const string Char = "char";

    /// <summary>
    /// Number type ID. Maps from .NET numeric types (int, long, float, double, decimal, etc.).
    /// Serializes to JSON number.
    /// </summary>
    public const string Number = "number";

    /// <summary>
    /// Boolean type ID. Maps from .NET <see cref="System.Boolean"/>.
    /// </summary>
    public const string Boolean = "boolean";

    /// <summary>
    /// Void type ID. Represents no return value.
    /// </summary>
    public const string Void = "void";

    #endregion

    #region Date/Time Type IDs

    /// <summary>
    /// DateTime type ID. Maps from .NET <see cref="System.DateTime"/>.
    /// Serializes to JSON string (ISO 8601).
    /// </summary>
    public const string DateTime = "datetime";

    /// <summary>
    /// DateTimeOffset type ID. Maps from .NET <see cref="System.DateTimeOffset"/>.
    /// Serializes to JSON string (ISO 8601).
    /// </summary>
    public const string DateTimeOffset = "datetimeoffset";

    /// <summary>
    /// DateOnly type ID. Maps from .NET <see cref="System.DateOnly"/>.
    /// Serializes to JSON string (YYYY-MM-DD).
    /// </summary>
    public const string DateOnly = "dateonly";

    /// <summary>
    /// TimeOnly type ID. Maps from .NET <see cref="System.TimeOnly"/>.
    /// Serializes to JSON string (HH:mm:ss).
    /// </summary>
    public const string TimeOnly = "timeonly";

    /// <summary>
    /// TimeSpan type ID. Maps from .NET <see cref="System.TimeSpan"/>.
    /// Serializes to JSON number (total milliseconds).
    /// </summary>
    public const string TimeSpan = "timespan";

    #endregion

    #region Other Scalar Type IDs

    /// <summary>
    /// Guid type ID. Maps from .NET <see cref="System.Guid"/>.
    /// Serializes to JSON string.
    /// </summary>
    public const string Guid = "guid";

    /// <summary>
    /// Uri type ID. Maps from .NET <see cref="System.Uri"/>.
    /// Serializes to JSON string.
    /// </summary>
    public const string Uri = "uri";

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

    #region Type Category Helpers

    /// <summary>
    /// Checks if a type ID represents a primitive type.
    /// </summary>
    /// <param name="typeId">The ATS type ID to check.</param>
    /// <returns>True if the type is a primitive.</returns>
    public static bool IsPrimitive(string? typeId) => typeId switch
    {
        String or Char or Number or Boolean or Void => true,
        DateTime or DateTimeOffset or DateOnly or TimeOnly or TimeSpan => true,
        Guid or Uri => true,
        _ => false
    };

    /// <summary>
    /// Checks if a type ID represents a handle type (has Assembly/Type format).
    /// </summary>
    /// <param name="typeId">The ATS type ID to check.</param>
    /// <returns>True if the type is a handle.</returns>
    public static bool IsHandle(string? typeId)
    {
        if (string.IsNullOrEmpty(typeId))
        {
            return false;
        }

        // Primitives are not handles
        if (IsPrimitive(typeId))
        {
            return false;
        }

        // Handle types have the format {AssemblyName}/{TypeName}
        return typeId.Contains('/');
    }

    /// <summary>
    /// Gets the type category for a type ID.
    /// Note: This method cannot distinguish DTOs from Handles based on type ID alone.
    /// Use the scanner's type mapping to determine if a handle type is actually a DTO.
    /// </summary>
    /// <param name="typeId">The ATS type ID.</param>
    /// <param name="isCallback">True if this is a callback parameter.</param>
    /// <returns>The type category.</returns>
    public static AtsTypeCategory GetCategory(string? typeId, bool isCallback = false)
    {
        if (isCallback)
        {
            return AtsTypeCategory.Callback;
        }

        if (IsPrimitive(typeId))
        {
            return AtsTypeCategory.Primitive;
        }

        // For handle-format types, we default to Handle.
        // The scanner/runtime can override this to Dto if the type has [AspireDto].
        return AtsTypeCategory.Handle;
    }

    #endregion
}
