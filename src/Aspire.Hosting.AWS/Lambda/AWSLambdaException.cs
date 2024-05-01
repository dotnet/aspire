// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.AWS.Lambda;

/// <summary>
///
/// </summary>
/// <param name="message"></param>
/// <param name="innerException"></param>
public class AWSLambdaException(string message, Exception? innerException = null)
    : Exception(message, innerException);
