// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting;

/// <summary>
/// Marks a method, type, or assembly-level type mapping as an ATS (Aspire Type System) export.
/// </summary>
/// <remarks>
/// <para>
/// This attribute serves two purposes:
/// </para>
/// <list type="number">
/// <item>
/// <description>
/// <b>Capability exports (on methods):</b> Marks a static method as an ATS capability.
/// Specify just the method name - the capability ID is computed as <c>{AssemblyName}/{methodName}</c>.
/// For example: <c>"addRedis"</c> in Aspire.Hosting.Redis becomes <c>Aspire.Hosting.Redis/addRedis</c>.
/// </description>
/// </item>
/// <item>
/// <description>
/// <b>Type mapping (on types or assembly):</b> Maps a CLR type to an ATS type ID.
/// Use <see cref="AtsTypeId"/> to specify the ATS type ID.
/// For types you don't own, use assembly-level with <see cref="Type"/> property.
/// </description>
/// </item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// // Capability export on a method - just specify the method name
/// [AspireExport("addRedis", Description = "Adds a Redis resource")]
/// public static IResourceBuilder&lt;RedisResource&gt; AddRedis(...) { }
/// // Scanner computes capability ID: Aspire.Hosting.Redis/addRedis
///
/// // Type mapping on a type you own
/// [AspireExport(AtsTypeId = "aspire/Redis")]
/// public class RedisResource : ContainerResource { }
///
/// // Assembly-level type mapping for types you don't own
/// [assembly: AspireExport(typeof(IDistributedApplicationBuilder), AtsTypeId = "aspire/Builder")]
/// </code>
/// </example>
[AttributeUsage(
    AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Assembly,
    Inherited = false,
    AllowMultiple = true)]
public sealed class AspireExportAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance for a capability export (on methods).
    /// </summary>
    /// <param name="id">
    /// The method name for this capability. The full capability ID is computed
    /// as <c>{AssemblyName}/{methodName}</c>.
    /// For example: <c>"addRedis"</c> in Aspire.Hosting.Redis becomes
    /// <c>Aspire.Hosting.Redis/addRedis</c>.
    /// </param>
    public AspireExportAttribute(string id)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
    }

    /// <summary>
    /// Initializes a new instance for a type mapping (on types or assembly-level).
    /// </summary>
    /// <remarks>
    /// Use this constructor when declaring a type mapping. Set <see cref="AtsTypeId"/> to specify
    /// the ATS type ID. For assembly-level mappings, also set <see cref="Type"/>.
    /// </remarks>
    public AspireExportAttribute()
    {
    }

    /// <summary>
    /// Initializes a new instance for an assembly-level type mapping.
    /// </summary>
    /// <param name="type">The CLR type to map to an ATS type ID.</param>
    /// <remarks>
    /// Use this constructor at assembly level for types you don't own.
    /// Set <see cref="AtsTypeId"/> to specify the ATS type ID.
    /// </remarks>
    /// <example>
    /// <code>
    /// [assembly: AspireExport(typeof(IDistributedApplicationBuilder), AtsTypeId = "aspire/Builder")]
    /// </code>
    /// </example>
    public AspireExportAttribute(Type type)
    {
        Type = type ?? throw new ArgumentNullException(nameof(type));
    }

    /// <summary>
    /// Gets the method name for capability exports.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The full capability ID is computed as <c>{AssemblyName}/{Id}</c>.
    /// </para>
    /// <para>
    /// This is null for type mappings.
    /// </para>
    /// </remarks>
    public string? Id { get; }

    /// <summary>
    /// Gets or sets the ATS type ID for type mappings.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Use this to explicitly map a CLR type to an ATS type ID.
    /// This avoids inference and string parsing in the scanner.
    /// </para>
    /// <para>
    /// Examples:
    /// <list type="bullet">
    /// <item><description><c>"aspire/Builder"</c> for IDistributedApplicationBuilder</description></item>
    /// <item><description><c>"aspire/Redis"</c> for RedisResource</description></item>
    /// <item><description><c>"aspire/IResourceWithEnvironment"</c> for IResourceWithEnvironment</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public string? AtsTypeId { get; set; }

    /// <summary>
    /// Gets or sets the CLR type for assembly-level type mappings.
    /// </summary>
    /// <remarks>
    /// Use this at assembly level to map types you don't own to ATS type IDs.
    /// </remarks>
    public Type? Type { get; set; }

    /// <summary>
    /// Gets or sets a description of what this export does.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the method name to use in generated polyglot SDKs.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When not specified, the method name from <see cref="Id"/> is used directly.
    /// </para>
    /// <para>
    /// Use this property to override the generated name when disambiguation is needed
    /// (e.g., to avoid collisions with another integration's method of the same name).
    /// Each language generator will apply its own formatting convention
    /// (camelCase for TypeScript, snake_case for Python, etc.).
    /// </para>
    /// </remarks>
    public string? MethodName { get; set; }
}
