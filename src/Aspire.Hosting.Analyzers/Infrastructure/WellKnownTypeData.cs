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
        Aspire_Hosting_AspireCallbackAttribute,
        Aspire_Hosting_ApplicationModel_IResource,
        Aspire_Hosting_ApplicationModel_IResourceBuilder_1,
        System_Threading_Tasks_Task,
        System_Threading_Tasks_Task_1
    }

    public static string[] WellKnownTypeNames = [
        "Aspire.Hosting.ApplicationModel.IModelNameParameter",
        "Aspire.Hosting.ApplicationModel.ResourceNameAttribute",
        "Aspire.Hosting.ApplicationModel.EndpointNameAttribute",
        "Aspire.Hosting.AspireExportAttribute",
        "Aspire.Hosting.AspireCallbackAttribute",
        "Aspire.Hosting.ApplicationModel.IResource",
        "Aspire.Hosting.ApplicationModel.IResourceBuilder`1",
        "System.Threading.Tasks.Task",
        "System.Threading.Tasks.Task`1"
    ];
}
