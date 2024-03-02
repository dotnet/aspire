// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.FluentUI.AspNetCore.Components;

namespace Aspire.Dashboard.Components.CustomIcons;

internal static class AspireIcons
{
    internal static class Size24
    {
        // The official SVGs from GitHub have a viewbox of 96x96, so we need to scale them down to 20x20 and center them within the 24x24 box to make them match the
        // other icons we're using. We also need to remove the fill attribute from the SVGs so that we can color them with CSS.
        internal sealed class GitHub : Icon { public GitHub() : base("GitHub", IconVariant.Regular, IconSize.Size24, @"<path transform=""scale(0.20833) translate(9.6 9.6)"" fill-rule=""evenodd"" clip-rule=""evenodd"" d=""M48.854 0C21.839 0 0 22 0 49.217c0 21.756 13.993 40.172 33.405 46.69 2.427.49 3.316-1.059 3.316-2.362 0-1.141-.08-5.052-.08-9.127-13.59 2.934-16.42-5.867-16.42-5.867-2.184-5.704-5.42-7.17-5.42-7.17-4.448-3.015.324-3.015.324-3.015 4.934.326 7.523 5.052 7.523 5.052 4.367 7.496 11.404 5.378 14.235 4.074.404-3.178 1.699-5.378 3.074-6.6-10.839-1.141-22.243-5.378-22.243-24.283 0-5.378 1.94-9.778 5.014-13.2-.485-1.222-2.184-6.275.486-13.038 0 0 4.125-1.304 13.426 5.052a46.97 46.97 0 0 1 12.214-1.63c4.125 0 8.33.571 12.213 1.63 9.302-6.356 13.427-5.052 13.427-5.052 2.67 6.763.97 11.816.485 13.038 3.155 3.422 5.015 7.822 5.015 13.2 0 18.905-11.404 23.06-22.324 24.283 1.78 1.548 3.316 4.481 3.316 9.126 0 6.6-.08 11.897-.08 13.526 0 1.304.89 2.853 3.316 2.364 19.412-6.52 33.405-24.935 33.405-46.691C97.707 22 75.788 0 48.854 0z"" />") { } }
        internal sealed class Logo : Icon { public Logo() : base("Logo", IconVariant.Regular, IconSize.Size24, @"<svg width=""24"" height=""24"" viewBox=""0 0 24 24"" fill=""none"" xmlns=""http://www.w3.org/2000/svg"">
    <mask id=""mask0_449_831"" style=""mask-type:alpha"" maskUnits=""userSpaceOnUse"" x=""0"" y=""0"" width=""24"" height=""22"">
        <path fill-rule=""evenodd"" clip-rule=""evenodd"" d=""M5.39001 12C4.49001 12 3.67 12.4799 3.22 13.2499L6.67 7.27994L6.6817 7.25982L9.84001 1.79005C10.05 1.43005 10.36 1.11005 10.75 0.880049C11.14 0.650049 11.57 0.550049 12 0.550049C12.86 0.550049 13.7 0.990049 14.17 1.80005L17.33 7.28005L23.67 18.25C23.88 18.62 24 19.05 24 19.5C24 20.88 22.88 22 21.5 22H8.27002C8.27001 22 8.27002 22 8.27002 22H2.5C1.12 22 0 20.88 0 19.5C0 19.05 0.12 18.62 0.33 18.25L3.22 13.2499C3.67 12.4799 4.49001 12 5.39001 12C5.39002 12 5.39001 12 5.39001 12Z"" fill=""url(#paint0_linear_449_831)""/>
    </mask>
    <g mask=""url(#mask0_449_831)"">
        <path d=""M20.06 12H13.72L11 7.28005C10.79 6.91005 10.48 6.59005 10.08 6.37005C8.88998 5.67005 7.35998 6.08005 6.66998 7.28005L9.83998 1.79005C10.05 1.43005 10.36 1.11005 10.75 0.880049C11.14 0.650049 11.57 0.550049 12 0.550049C12.86 0.550049 13.7 0.990049 14.17 1.80005L17.33 7.28005L20.06 12Z"" fill=""url(#paint1_linear_449_831)""/>
        <g filter=""url(#filter0_dd_449_831)"">
            <path d=""M5.38997 11.9999H13.72L11 7.27994C10.79 6.90994 10.48 6.58994 10.08 6.36994C8.88997 5.66994 7.35997 6.07994 6.66997 7.27994L3.21997 13.2499C3.66997 12.4799 4.48997 11.9999 5.38997 11.9999Z"" fill=""url(#paint2_linear_449_831)""/>
            <path d=""M21.5 22C22.88 22 24 20.88 24 19.5C24 19.05 23.88 18.62 23.67 18.25L20.06 12L13.72 11.9999L17.33 18.25C17.55 18.62 17.67 19.05 17.67 19.5C17.67 20.88 16.55 22 15.17 22H21.5Z"" fill=""url(#paint3_linear_449_831)""/>
        </g>
        <g filter=""url(#filter1_dd_449_831)"">
            <path d=""M17.67 19.5C17.67 20.88 16.55 22 15.17 22H8.27002C9.65002 22 10.77 20.88 10.77 19.5C10.77 19.05 10.65 18.62 10.44 18.25L7.55001 13.25C7.52002 13.19 7.48001 13.14 7.44001 13.08C6.99001 12.42 6.23001 12 5.39001 12H13.72L17.33 18.25C17.55 18.62 17.67 19.05 17.67 19.5Z"" fill=""url(#paint4_linear_449_831)""/>
        </g>
        <g filter=""url(#filter2_dd_449_831)"">
            <path d=""M10.77 19.5C10.77 20.88 9.65 22 8.27 22H2.5C1.12 22 0 20.88 0 19.5C0 19.05 0.12 18.62 0.33 18.25L3.22 13.25C3.67 12.48 4.49 12 5.39 12C6.23 12 6.99 12.42 7.44 13.08C7.48 13.14 7.52 13.19 7.55 13.25L10.44 18.25C10.65 18.62 10.77 19.05 10.77 19.5Z"" fill=""url(#paint5_linear_449_831)""/>
        </g>
    </g>
    <defs>
        <filter id=""filter0_dd_449_831"" x=""1.21997"" y=""4.52808"" width=""24.78"" height=""19.9719"" filterUnits=""userSpaceOnUse"" color-interpolation-filters=""sRGB"">
            <feFlood flood-opacity=""0"" result=""BackgroundImageFix""/>
            <feColorMatrix in=""SourceAlpha"" type=""matrix"" values=""0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 127 0"" result=""hardAlpha""/>
            <feOffset dy=""0.095""/>
            <feGaussianBlur stdDeviation=""0.095""/>
            <feColorMatrix type=""matrix"" values=""0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0.24 0""/>
            <feBlend mode=""normal"" in2=""BackgroundImageFix"" result=""effect1_dropShadow_449_831""/>
            <feColorMatrix in=""SourceAlpha"" type=""matrix"" values=""0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 127 0"" result=""hardAlpha""/>
            <feOffset dy=""0.5""/>
            <feGaussianBlur stdDeviation=""1""/>
            <feColorMatrix type=""matrix"" values=""0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0.32 0""/>
            <feBlend mode=""normal"" in2=""effect1_dropShadow_449_831"" result=""effect2_dropShadow_449_831""/>
            <feBlend mode=""normal"" in=""SourceGraphic"" in2=""effect2_dropShadow_449_831"" result=""shape""/>
        </filter>
        <filter id=""filter1_dd_449_831"" x=""3.39001"" y=""10.5"" width=""16.28"" height=""14"" filterUnits=""userSpaceOnUse"" color-interpolation-filters=""sRGB"">
            <feFlood flood-opacity=""0"" result=""BackgroundImageFix""/>
            <feColorMatrix in=""SourceAlpha"" type=""matrix"" values=""0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 127 0"" result=""hardAlpha""/>
            <feOffset dy=""0.095""/>
            <feGaussianBlur stdDeviation=""0.095""/>
            <feColorMatrix type=""matrix"" values=""0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0.24 0""/>
            <feBlend mode=""normal"" in2=""BackgroundImageFix"" result=""effect1_dropShadow_449_831""/>
            <feColorMatrix in=""SourceAlpha"" type=""matrix"" values=""0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 127 0"" result=""hardAlpha""/>
            <feOffset dy=""0.5""/>
            <feGaussianBlur stdDeviation=""1""/>
            <feColorMatrix type=""matrix"" values=""0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0.32 0""/>
            <feBlend mode=""normal"" in2=""effect1_dropShadow_449_831"" result=""effect2_dropShadow_449_831""/>
            <feBlend mode=""normal"" in=""SourceGraphic"" in2=""effect2_dropShadow_449_831"" result=""shape""/>
        </filter>
        <filter id=""filter2_dd_449_831"" x=""-2"" y=""10.5"" width=""14.77"" height=""14"" filterUnits=""userSpaceOnUse"" color-interpolation-filters=""sRGB"">
            <feFlood flood-opacity=""0"" result=""BackgroundImageFix""/>
            <feColorMatrix in=""SourceAlpha"" type=""matrix"" values=""0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 127 0"" result=""hardAlpha""/>
            <feOffset dy=""0.095""/>
            <feGaussianBlur stdDeviation=""0.095""/>
            <feColorMatrix type=""matrix"" values=""0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0.24 0""/>
            <feBlend mode=""normal"" in2=""BackgroundImageFix"" result=""effect1_dropShadow_449_831""/>
            <feColorMatrix in=""SourceAlpha"" type=""matrix"" values=""0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 127 0"" result=""hardAlpha""/>
            <feOffset dy=""0.5""/>
            <feGaussianBlur stdDeviation=""1""/>
            <feColorMatrix type=""matrix"" values=""0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0.32 0""/>
            <feBlend mode=""normal"" in2=""effect1_dropShadow_449_831"" result=""effect2_dropShadow_449_831""/>
            <feBlend mode=""normal"" in=""SourceGraphic"" in2=""effect2_dropShadow_449_831"" result=""shape""/>
        </filter>
        <linearGradient id=""paint0_linear_449_831"" x1=""1.88475"" y1=""11.1667"" x2=""10.31"" y2=""23.1443"" gradientUnits=""userSpaceOnUse"">
            <stop stop-color=""#CBBFF2""/>
            <stop offset=""1"" stop-color=""#B9AAEE""/>
        </linearGradient>
        <linearGradient id=""paint1_linear_449_831"" x1=""9.6127"" y1=""-0.685575"" x2=""16.8764"" y2=""13.8912"" gradientUnits=""userSpaceOnUse"">
            <stop stop-color=""#7455DD""/>
            <stop stop-color=""#6745DA""/>
            <stop offset=""1"" stop-color=""#512BD4""/>
        </linearGradient>
        <linearGradient id=""paint2_linear_449_831"" x1=""7.90532"" y1=""3.78438"" x2=""19.1767"" y2=""23.0023"" gradientUnits=""userSpaceOnUse"">
            <stop stop-color=""#856AE1""/>
            <stop offset=""1"" stop-color=""#7455DD""/>
        </linearGradient>
        <linearGradient id=""paint3_linear_449_831"" x1=""7.90532"" y1=""3.78438"" x2=""19.1767"" y2=""23.0023"" gradientUnits=""userSpaceOnUse"">
            <stop stop-color=""#856AE1""/>
            <stop offset=""1"" stop-color=""#7455DD""/>
        </linearGradient>
        <linearGradient id=""paint4_linear_449_831"" x1=""5.4257"" y1=""9.22222"" x2=""13.2216"" y2=""21.4193"" gradientUnits=""userSpaceOnUse"">
            <stop stop-color=""#A895E9""/>
            <stop offset=""1"" stop-color=""#9780E5""/>
        </linearGradient>
        <linearGradient id=""paint5_linear_449_831"" x1=""1.88475"" y1=""11.1667"" x2=""10.31"" y2=""23.1443"" gradientUnits=""userSpaceOnUse"">
            <stop stop-color=""#CBBFF2""/>
            <stop offset=""1"" stop-color=""#B9AAEE""/>
        </linearGradient>
    </defs>
</svg>
") { } }
    }
}
