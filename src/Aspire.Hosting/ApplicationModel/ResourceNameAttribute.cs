// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Signifies that a parameter represents a resource name.
/// <para>
/// Decorate <see cref="string"/> parameters representing resource names with this attribute, e.g.:
/// <code lang="csharp">
/// public static IResourceBuilder&lt;MyResource&gt; AddCustomResource(this IDistributedApplicationBuilder builder, [ResourceName] string name)
/// </code>
/// </para>
/// </summary>
/// <remarks>
/// This API supports analyzers in Aspire.Hosting.Analyzers.
/// </remarks>
[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
public sealed class ResourceNameAttribute : Attribute, IModelNameParameter
{

}
