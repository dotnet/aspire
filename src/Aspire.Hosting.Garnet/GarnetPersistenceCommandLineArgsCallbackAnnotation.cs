// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Garnet;

internal sealed class GarnetPersistenceCommandLineArgsCallbackAnnotation(TimeSpan interval, long keysChangedThreshold)
    : CommandLineArgsCallbackAnnotation(context =>
    {
        context.Args.Add("--save");
        context.Args.Add(interval.TotalSeconds.ToString(CultureInfo.InvariantCulture));
        context.Args.Add(keysChangedThreshold.ToString(CultureInfo.InvariantCulture));
        return Task.CompletedTask;
    });
