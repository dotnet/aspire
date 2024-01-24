// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

var testProgram = TestProgram.Create<Program>(args, includeIntegrationServices: true, disableDashboard: false, includeNodeApp: true);
await testProgram.RunAsync();
