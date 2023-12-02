// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Utils;

internal static class PasswordUtil
{
    internal static string EscapePassword(string password) => password.Replace("\"", "\"\"");
}
