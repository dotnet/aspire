// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This is the entry point for the pre-built AppHost server used in bundle mode.
// It runs the RemoteHostServer which listens on a Unix socket for JSON-RPC
// connections from polyglot app hosts (TypeScript, Python, etc.)

await Aspire.Hosting.RemoteHost.RemoteHostServer.RunAsync(args).ConfigureAwait(false);
