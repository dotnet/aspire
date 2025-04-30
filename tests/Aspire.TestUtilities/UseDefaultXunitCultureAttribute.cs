// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;

namespace Aspire.TestUtilities;

/// <summary>
///  Apply this attribute to your test method to replace the <see cref="Thread.CurrentThread" />
///  <see cref="CultureInfo.CurrentCulture" /> with another culture.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class UseDefaultXunitCultureAttribute : UseCultureAttribute
{
    public const string DefaultXunitCultureAttribute = "en-US";

    /// <summary>
    ///  Replaces the culture of the current thread with the <see cref="DefaultXunitCultureAttribute"/>.
    /// </summary>
    public UseDefaultXunitCultureAttribute()
        : base(DefaultXunitCultureAttribute, DefaultXunitCultureAttribute)
    {
    }
}
