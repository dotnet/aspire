// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Reflection;

namespace Aspire.TestUtilities;

/// <summary>
///  Apply this attribute to your test method to replace the <see cref="Thread.CurrentThread" />
///  <see cref="CultureInfo.CurrentCulture" /> with another culture.
/// </summary>
/// <remarks>
///  Replaces the culture of the current thread with <paramref name="culture" />.
/// </remarks>
/// <param name="culture">The name of the culture to set for both <see cref="Culture" />.</param>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public abstract class UseCultureAttribute(string culture, string uiCulture) : BeforeAfterTestAttribute
{
    private CultureInfo _originalCulture = Thread.CurrentThread.CurrentCulture;
    private CultureInfo _originalUICulture = Thread.CurrentThread.CurrentUICulture;

    private readonly Lazy<CultureInfo> _culture = new(() => new(culture, useUserOverride: false));
    private readonly Lazy<CultureInfo> _uiCulture = new(() => new(uiCulture, useUserOverride: false));

    /// <summary>
    /// Gets the culture.
    /// </summary>
    public CultureInfo Culture => _culture.Value;

    /// <summary>
    /// Gets the UI culture.
    /// </summary>
    public CultureInfo UICulture => _uiCulture.Value;

    /// <summary>
    ///  Replaces the culture and UI culture of the current thread with <paramref name="culture" />.
    /// </summary>
    /// <param name="culture">The name of the culture to set for both <see cref="Culture" /> and <see cref="UICulture" />.</param>
    protected UseCultureAttribute(string culture)
        : this(culture, culture)
    {
    }

    /// <summary>
    /// Stores the current <see cref="Thread.CurrentPrincipal" />
    /// <see cref="CultureInfo.CurrentCulture" />
    /// and replaces them with the new cultures defined in the constructor.
    /// </summary>
    /// <param name="methodUnderTest">The method under test</param>
    public override void Before(MethodInfo methodUnderTest, IXunitTest test)
    {
        _originalCulture = Thread.CurrentThread.CurrentCulture;
        _originalUICulture = Thread.CurrentThread.CurrentUICulture;

        CultureInfo.DefaultThreadCurrentCulture = Culture;
        CultureInfo.DefaultThreadCurrentUICulture = UICulture;

        Thread.CurrentThread.CurrentCulture = Culture;
        Thread.CurrentThread.CurrentUICulture = UICulture;

        CultureInfo.CurrentCulture.ClearCachedData();
        CultureInfo.CurrentUICulture.ClearCachedData();

        base.Before(methodUnderTest, test);
    }

    /// <summary>
    /// Restores the original <see cref="CultureInfo.CurrentCulture" /> and
    /// <see cref="CultureInfo.CurrentUICulture" /> to <see cref="Thread.CurrentPrincipal" />
    /// </summary>
    /// <param name="methodUnderTest">The method under test</param>
    public override void After(MethodInfo methodUnderTest, IXunitTest test)
    {
        Thread.CurrentThread.CurrentCulture = _originalCulture;
        Thread.CurrentThread.CurrentUICulture = _originalUICulture;

        CultureInfo.CurrentCulture.ClearCachedData();
        CultureInfo.CurrentUICulture.ClearCachedData();

        base.After(methodUnderTest, test);
    }
}
