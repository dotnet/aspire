// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace Aspire.Confluent.Kafka.Tests;

public static class Program
{
    [ModuleInitializer]
    public static void Initialize()
    {
        AppContext.SetSwitch("EnableAspire8ConfluentKafkaMetrics", true);
    }
}

