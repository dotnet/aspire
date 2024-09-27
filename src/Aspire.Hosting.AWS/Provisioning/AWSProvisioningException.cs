// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.AWS.Provisioning;

/// <summary>
/// Exception for errors provisioning AWS application resources
/// </summary>
/// <param name="message"></param>
/// <param name="innerException"></param>
public class AWSProvisioningException(string message, Exception? innerException = null) : Exception(message, innerException)
{
}
