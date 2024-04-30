// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Amazon.CDK;

namespace Aspire.Hosting.AWS.CDK;

/// <summary>
/// Delegate for building an AWS CDK stack
/// </summary>
/// <typeparam name="T">Construct type</typeparam>
public delegate T StackBuilderDelegate<out T>(App app) where T : Stack;
