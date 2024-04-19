// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.RegularExpressions;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// An implementation of <see cref="Dictionary{TKey, TValue}"/> that only allows keys that are valid environment variable names.
/// </summary>
public partial class EnvironmentVariableDictionary : ValidatingDictionary<string, object>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EnvironmentVariableDictionary"/> class.
    /// </summary>
    public EnvironmentVariableDictionary() : base(ValidateKey, null)
    {
    }

    [GeneratedRegex(@"^[A-Za-z_][A-Za-z0-9_]*$", RegexOptions.Singleline | RegexOptions.CultureInvariant)]
    private static partial Regex EnvironmentVariableRegex();

    private static bool ValidateKey(string key) => EnvironmentVariableRegex().IsMatch(key);
}
