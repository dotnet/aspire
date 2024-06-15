// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Constructs;

namespace Aspire.Hosting.AWS.CDK;

/// <summary>
/// Delegate for building an AWS CDK construct
/// </summary>
/// <typeparam name="T">Construct type</typeparam>
public delegate T ConstructBuilderDelegate<out T>(Construct scope) where T : IConstruct;
