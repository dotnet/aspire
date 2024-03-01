// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Aspire.Workload.Tests;

internal static class ProcessExtensions
{
    public static Task StartAndWaitForExitAsync(this Process subject)
    {
        var taskCompletionSource = new TaskCompletionSource<object>();

        try
        {
            subject.EnableRaisingEvents = true;

            subject.Exited += (s, a) =>
            {
                //Console.WriteLine ($"StartAndWaitForExitAsync: got Exited event");
                taskCompletionSource.SetResult(new object());
            };

            subject.Start();
        }
        catch (Exception ex)
        {
            //Console.WriteLine ($"-- StartAndWaitForExitAsync threw.. setting exception on the tcs");
            taskCompletionSource.SetException(ex);
        }

        return taskCompletionSource.Task;
    }
}
