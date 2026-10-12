// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Native.Client;
using Microsoft.Extensions.Logging.Abstractions;
using NativeIntegration.Generated;

await NativeProviderHost.RunAsync(ProviderContract.Create(), TimeProvider.System, NullLogger.Instance, CancellationToken.None);
