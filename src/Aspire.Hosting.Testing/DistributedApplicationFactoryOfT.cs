// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
namespace Aspire.Hosting.Testing;

/// <summary>
/// Factory for creating a distributed application for testing.
/// </summary>
/// <typeparam name="TEntryPoint">
/// A type in the entry point assembly of the target Aspire AppHost. Typically, the Program class can be used.
/// </typeparam>
public class DistributedApplicationFactory<TEntryPoint>() : DistributedApplicationFactory(typeof(TEntryPoint)) where TEntryPoint : class
{

}
