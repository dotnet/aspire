// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Hex1b;
using Hex1b.Widgets;

namespace Aspire.Cli.UI;

/// <summary>
/// Splash screen for the Aspire Monitor TUI using braille-art rendering.
/// </summary>
internal static class AspireMonitorSplash
{
    public const int SplashDurationMs = 2000;

    // Braille-art representation of ".NET Aspire" logo
    private static readonly string[] s_logoLines =
    [
        "⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿",
        "⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿",
        "⣿⣿⠋⠀⠀⠀⠀⠙⣿⣿⡿⠋⠀⠀⠀⠀⠙⣿⡿⠋⠀⠀⠀⠀⢻⣿⣿⣿⣿⠋⠀⠀⠀⠀⠀⠀⠀⣿⡏⠀⠀⠀⠀⢹⣿⠉⠀⠀⠀⠀⠉⣿⣿⡟⠀⠀⠀⣿",
        "⣿⣿⠀⣶⣶⣶⣶⠀⢸⣿⡇⠀⣶⣶⣶⣶⠀⢸⡇⠀⣶⣶⣶⣦⠀⣿⣿⣿⣿⠀⣶⣶⣶⣶⣶⣶⠀⣿⡇⠀⣶⣶⣶⡀⣿⠀⣶⣶⣶⣶⠀⢸⣿⡇⠀⣿⠀⣿",
        "⣿⣿⠀⣿⣿⣿⣿⠀⢸⣿⡇⠀⣿⣿⣿⣿⠀⢸⡇⠀⣿⣿⣿⣿⠀⣿⣿⣿⣿⠀⣿⣿⣿⣿⣿⣿⠀⣿⡇⠀⣿⣿⣿⣷⢹⠀⣿⣿⣿⣿⠀⢸⣿⡇⠀⣿⠀⣿",
        "⣿⣿⠀⣿⣿⣿⣿⠀⢸⣿⡇⠀⣿⣿⣿⣿⠀⢸⡇⠀⣿⣿⣿⠏⢀⣿⣿⣿⣿⠀⣿⣿⣿⡏⠀⠀⠀⣿⡇⠀⣿⣿⣿⣿⢸⠀⣿⣿⠀⠀⠀⠀⣿⡇⠀⣿⠀⣿",
        "⣿⣿⠀⣿⣿⣿⣿⠀⢸⣿⡇⠀⣿⣿⣿⣿⠀⢸⡇⠀⠀⠀⠀⣠⣿⣿⣿⣿⣿⠀⣿⣿⣿⣿⣿⣶⠀⣿⡇⠀⣿⣿⣿⣿⣿⠀⣿⣿⣿⣿⣶⠀⣿⡇⠀⣿⠀⣿",
        "⣿⣿⠀⣿⣿⣿⣿⠀⢸⣿⡇⠀⣿⣿⣿⣿⠀⢸⡇⠀⣿⣿⣿⣿⠀⣿⣿⣿⣿⠀⣿⣿⣿⣿⣿⣿⠀⣿⡇⠀⣿⣿⣿⡿⣿⠀⣿⣿⣿⣿⣿⠀⣿⡇⠀⣿⠀⣿",
        "⣿⣿⠀⣿⣿⣿⣿⠀⢸⣿⡇⠀⣿⣿⣿⣿⠀⢸⡇⠀⣿⣿⣿⣿⠀⣿⣿⣿⣿⠀⣿⣿⣿⣿⣿⣿⠀⣿⡇⠀⣿⣿⣿⠁⣿⠀⣿⣿⣿⣿⣿⠀⣿⡇⠀⣿⠀⣿",
        "⣿⣿⠀⠛⠛⠛⠛⠀⢸⣿⡇⠀⠛⠛⠛⠛⠀⢸⡇⠀⠛⠛⠛⠛⠀⣿⣿⣿⣿⠀⠛⠛⠛⠛⠛⠛⠀⣿⡇⠀⠛⠛⠛⠀⣿⠀⠛⠛⠛⠛⠛⠀⣿⡇⠀⣿⠀⣿",
        "⣿⣿⣄⣀⣀⣀⣀⣠⣼⣿⣧⣄⣀⣀⣀⣀⣠⣼⣧⣄⣀⣀⣀⣀⣼⣿⣿⣿⣿⣄⣀⣀⣀⣀⣀⣀⣀⣿⣧⣄⣀⣀⣀⣼⣿⣄⣀⣀⣀⣀⣀⣤⣿⣿⣄⣿⣀⣿",
        "⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿",
        "⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿",
    ];

    public static Hex1bWidget Build(RootContext ctx)
    {
        return ctx.VStack(outer => [
            outer.Text("").Fill(),
            outer.Center(
                ctx.Surface(layerCtx => [
                    layerCtx.Layer((surface) =>
                    {
                        for (var row = 0; row < s_logoLines.Length && row < surface.Height; row++)
                        {
                            var line = s_logoLines[row];
                            surface.WriteText(0, row, line);
                        }
                    })
                ]).Size(56, s_logoLines.Length)
            ),
            outer.Text("").Fill()
        ]).Fill();
    }
}
