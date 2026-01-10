// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Ats;

/// <summary>
/// Marks a parameter as passthrough, bypassing ATS type validation.
/// This is for internal use only by collection intrinsics (Dict, List operations).
/// The parameter will be marshalled/unmarshalled dynamically at runtime.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter)]
internal sealed class AtsPassthroughAttribute : Attribute
{
}
