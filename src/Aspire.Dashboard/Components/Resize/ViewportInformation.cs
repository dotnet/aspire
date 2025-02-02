// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Dashboard.Components.Resize;

/// <param name="IsDesktop">Whether the viewport is desktop-sized</param>
/// <param name="IsUltraLowHeight">Ultra low height is users with very high zooms and/or very low resolutions,
/// where the height is significantly constrained. In these cases, the users need the entire main page content
/// (toolbar, title, main content, footer) to be scrollable, rather than just the main content.
/// </param>
/// <param name="IsUltraLowWidth">Ultra low width means users with extremely constricted viewport widths, requiring
/// the disabling of horizontal space-intensive non-essential features such as the copy button in GridValue, so as
/// to preserve maximum space for content.</param>
public record ViewportInformation(bool IsDesktop, bool IsUltraLowHeight, bool IsUltraLowWidth)
{
    // Set our mobile cutoff at 768 pixels, which is ~medium tablet size
    internal const int MobileCutoffPixelWidth = 768;

    // A small enough height that the filters button takes up too much of the vertical space on screen
    internal const int LowHeightCutoffPixelWidth = 400;

    // Very close to the minimum width we need to support (320px)
    internal const int LowWidthCutoffPixelWidth = 350;

    public static ViewportInformation GetViewportInformation(ViewportSize viewportSize)
    {
        return new ViewportInformation(
            IsDesktop: viewportSize.Width > MobileCutoffPixelWidth,
            IsUltraLowHeight: viewportSize.Height < LowHeightCutoffPixelWidth,
            IsUltraLowWidth: viewportSize.Width < LowWidthCutoffPixelWidth);
    }
}
