// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Aspire.EndToEnd.Tests;

internal static class ProcessExtensions
{
    // private static int RunProcessAndWaitForExit(string fileName, string arguments, TimeSpan timeout, out string stdout)
    // {
    //     var startInfo = new ProcessStartInfo
    //     {
    //         FileName = fileName,
    //         Arguments = arguments,
    //         RedirectStandardOutput = true,
    //         UseShellExecute = false
    //     };

    //     var process = Process.Start(startInfo);

    //     stdout = null;
    //     if (process.WaitForExit((int)timeout.TotalMilliseconds))
    //     {
    //         stdout = process.StandardOutput.ReadToEnd();
    //         return process.ExitCode;
    //     }

    //     process.Kill(entireProcessTree: true);
    //     // sigkill - 128+9(sigkill)
    //     return 137;
    // }

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
