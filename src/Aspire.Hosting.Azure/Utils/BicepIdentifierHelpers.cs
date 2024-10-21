// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Azure.Provisioning;

namespace Aspire.Hosting.Azure.Utils;

internal static class BicepIdentifierHelpers
{
    internal static string ThrowIfInvalid(string name, [CallerArgumentExpression(nameof(name))] string? paramName = null)
    {
        Infrastructure.ValidateBicepIdentifier(name, paramName);
        return name;
    }
}
