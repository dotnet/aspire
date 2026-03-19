// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Health;

/// <summary>
/// Constants used for health check data exchange between <see cref="AspireHttpHealthCheck"/>
/// and <see cref="ResourceHealthCheckService"/>.
/// </summary>
internal static class HealthCheckConstants
{
    /// <summary>
    /// Data dictionary keys used to pass expanded health check entries between components.
    /// </summary>
    internal static class DataKeys
    {
        /// <summary>
        /// Key indicating that a health check result contains multiple individual health check entries.
        /// </summary>
        public const string MultipleHealthChecks = "__AspireMultipleHealthChecks";

        /// <summary>
        /// Key for the dictionary of individual <see cref="Microsoft.Extensions.Diagnostics.HealthChecks.HealthReportEntry"/> sub-entries.
        /// </summary>
        public const string SubEntries = "SubEntries";
    }

    /// <summary>
    /// JSON property names used when parsing Aspire health check responses.
    /// </summary>
    internal static class JsonProperties
    {
        public const string Status = "status";
        public const string Entries = "entries";
        public const string Description = "description";
        public const string Duration = "duration";
        public const string Exception = "exception";
        public const string Data = "data";
    }
}
