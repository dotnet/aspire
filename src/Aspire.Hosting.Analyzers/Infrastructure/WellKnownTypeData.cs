// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Analyzers.Infrastructure;

internal static class WellKnownTypeData
{
    public enum WellKnownType
    {
        Aspire_Hosting_ApplicationModel_IModelNameParameter,
        Aspire_Hosting_ApplicationModel_ResourceNameAttribute,
        Aspire_Hosting_ApplicationModel_EndpointNameAttribute,
        Aspire_Hosting_AspireExportAttribute,
        Aspire_Hosting_ApplicationModel_IResource,
        Aspire_Hosting_ApplicationModel_IResourceBuilder_1,
        System_Threading_Tasks_Task,
        System_Threading_Tasks_Task_1,

        // Date/time and scalar types
        System_DateTimeOffset,
        System_TimeSpan,
        System_DateOnly,
        System_TimeOnly,
        System_Guid,
        System_Uri,

        // Collection types
        System_Collections_Generic_Dictionary_2,
        System_Collections_Generic_IDictionary_2,
        System_Collections_Generic_List_1,
        System_Collections_Generic_IList_1,
        System_Collections_Generic_IReadOnlyList_1,
        System_Collections_Generic_IReadOnlyCollection_1,
        System_Collections_Generic_IReadOnlyDictionary_2
    }

    public static string[] WellKnownTypeNames = [
        "Aspire.Hosting.ApplicationModel.IModelNameParameter",
        "Aspire.Hosting.ApplicationModel.ResourceNameAttribute",
        "Aspire.Hosting.ApplicationModel.EndpointNameAttribute",
        "Aspire.Hosting.AspireExportAttribute",
        "Aspire.Hosting.ApplicationModel.IResource",
        "Aspire.Hosting.ApplicationModel.IResourceBuilder`1",
        "System.Threading.Tasks.Task",
        "System.Threading.Tasks.Task`1",

        // Date/time and scalar types
        "System.DateTimeOffset",
        "System.TimeSpan",
        "System.DateOnly",
        "System.TimeOnly",
        "System.Guid",
        "System.Uri",

        // Collection types
        "System.Collections.Generic.Dictionary`2",
        "System.Collections.Generic.IDictionary`2",
        "System.Collections.Generic.List`1",
        "System.Collections.Generic.IList`1",
        "System.Collections.Generic.IReadOnlyList`1",
        "System.Collections.Generic.IReadOnlyCollection`1",
        "System.Collections.Generic.IReadOnlyDictionary`2"
    ];
}
