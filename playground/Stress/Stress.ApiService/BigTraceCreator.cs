// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Stress.ApiService;

public class BigTraceCreator
{
    public const string ActivitySourceName = "BigTraceSpan";

    private static readonly ActivitySource s_activitySource = new(ActivitySourceName);

    public async Task CreateBigTraceAsync()
    {
        var activityStack = new Stack<Activity>();

        for (var i = 0; i < 10; i++)
        {
            var name = $"Span-{i}";
            using var activity = s_activitySource.StartActivity(name, ActivityKind.Client);
            if (activity == null)
            {
                continue;
            }

            await CreateChildActivityAsync(name);

            await Task.Delay(Random.Shared.Next(10, 50));
        }

        while (activityStack.Count > 0)
        {
            activityStack.Pop().Stop();
        }
    }

    private static async Task CreateChildActivityAsync(string parentName)
    {
        if (Random.Shared.NextDouble() > 0.05)
        {
            var name = parentName + "-0";
            using var activity = s_activitySource.StartActivity(name, ActivityKind.Client);

            await Task.Delay(Random.Shared.Next(10, 50));

            await CreateChildActivityAsync(name);

            await Task.Delay(Random.Shared.Next(10, 50));
        }
    }
}
