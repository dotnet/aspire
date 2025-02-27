// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;

var rootCommand = new RootCommand(".NET Aspire CLI");
var result = rootCommand.Parse(args);
var exitCode = await result.InvokeAsync().ConfigureAwait(false);
return exitCode;