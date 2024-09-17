// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Analyzers.Infrastructure;

internal static class WellKnownTypeData
{
    public enum WellKnownType
    {
        Aspire_Hosting_ApplicationModel_IModelNameParameter,
        Aspire_Hosting_ApplicationModel_ResourceNameAttribute,
        Aspire_Hosting_ApplicationModel_EndpointNameAttribute
    }

    public static string[] WellKnownTypeNames = [
        "Aspire.Hosting.ApplicationModel.IModelNameParameter",
        "Aspire.Hosting.ApplicationModel.ResourceNameAttribute",
        "Aspire.Hosting.ApplicationModel.EndpointNameAttribute"
    ];
}
