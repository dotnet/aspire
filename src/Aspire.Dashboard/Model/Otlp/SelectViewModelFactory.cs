// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Otlp.Model;

namespace Aspire.Dashboard.Model.Otlp;

public class SelectViewModelFactory
{
    public static List<SelectViewModel<string>> CreateApplicationsSelectViewModel(List<OtlpApplication> applications)
    {
        var byInstanceCount = applications.GroupBy(a => a.ApplicationName).ToDictionary(g => g.Key, g => g.Count());
        var retval = applications.Select(a =>
        {
            var name = byInstanceCount[a.ApplicationName] > 1 ?
                $"{a.ApplicationName} ({a.InstanceId.Substring(0, Math.Min(a.InstanceId.Length, 8))})"
                : a.ApplicationName;
            return new SelectViewModel<string> { Id = a.InstanceId, Name = name };
        }).ToList();
        return retval;
    }
}
