// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Constructs;

namespace Aspire.Hosting.AWS.CDK;

/// <summary>
///
/// </summary>
/// <typeparam name="T"></typeparam>
public delegate string ConstructOutputDelegate<in T>(T construct) where T : IConstruct;
