// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Otlp.Model;

namespace Aspire.Dashboard.Model.Otlp;

public class SelectViewModelFactory
{
    public static List<SelectViewModel<string>> CreateApplicationsSelectViewModel(List<OtlpApplication> applications)
    {
        var retval = applications.Select(a =>
        {
        var name = $"{a.ApplicationName} ({a.InstanceId.Substring(0, Math.Min(a.InstanceId.Length, 8))})";
            return new SelectViewModel<string> { Id = a.InstanceId, Name = name };
        }).ToList();
        return retval;
    }
}
