// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Nats.Client;
using Aspire;

[assembly: ConfigurationSchema("Aspire:Nats:Client", typeof(NatsClientSettings))]

[assembly: LoggingCategories("NATS.Client")]
