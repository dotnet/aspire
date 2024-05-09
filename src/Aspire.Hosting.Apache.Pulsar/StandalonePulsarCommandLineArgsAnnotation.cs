// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Apache.Pulsar;

internal sealed class StandalonePulsarCommandLineArgsAnnotation()
    : CommandLineArgsCallbackAnnotation(context =>
    {
        context.Args.Add("-c");
        context.Args.Add("bin/apply-config-from-env.py conf/standalone.conf && " +
                         "bin/pulsar standalone");

        return Task.CompletedTask;
    });
