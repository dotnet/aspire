// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

var testProgram = TestProgram.Create<Program>(args, includeIntegrationServices: true, disableDashboard: false, includeNodeApp: true, testProjectBasePath: Environment.GetEnvironmentVariable("ASPIRE_HOSTING_TEST_PROJECT_BASE_PATH"));
await testProgram.RunAsync();
