// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Confluent.Kafka;
using Producer;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

builder.AddKafkaProducer<string, string>("kafka");
builder.AddKafkaProducer<Null, string>("kafka");

builder.Services.AddHostedService<IntermittentProducerWorker>();
builder.Services.AddHostedService<ContinuousProducerWorker>();

builder.Build().Run();
