// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using ProjectMetadataGenerator;

var rootCommand = RootGenerateCommand.GetCommand();
return await rootCommand.Parse(args).InvokeAsync(CancellationToken.None).ConfigureAwait(false);
