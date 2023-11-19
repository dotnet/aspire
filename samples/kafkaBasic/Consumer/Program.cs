// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Confluent.Kafka;
using Consumer;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

builder.AddKafkaConsumer<Ignore, string>("kafka");

builder.Services.AddHostedService<ConsumerWorker>();

builder.Build().Run();
