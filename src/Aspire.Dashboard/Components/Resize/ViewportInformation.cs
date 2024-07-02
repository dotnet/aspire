// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Dashboard.Components.Resize;

/// <param name="IsDesktop">Set our mobile cutoff at 768 pixels, which is ~medium tablet size</param>
/// <param name="IsUltraLowHeight">Ultra low height is users with very high zooms and/or very low resolutions,
/// where the height is significantly constrained. In these cases, the users need the entire main page content
/// (toolbar, title, main content, footer) to be scrollable, rather than just the main content.
/// </param>
public record ViewportInformation(bool IsDesktop, bool IsUltraLowHeight);
