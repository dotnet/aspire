// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Hex1b.Surfaces;
using Hex1b.Theming;

namespace Aspire.Cli.UI;

/// <summary>
/// Transition effect that reveals the TUI from black after the splash screen.
/// Borders and structural characters fade in first from the bottom up,
/// then alphanumeric text appears as rapidly scrambled characters before
/// settling into the actual content.
/// </summary>
internal sealed class HackRevealEffect
{
    private struct CellInfo
    {
        public bool Initialized;
        public bool HasContent;
        public bool IsAlphaNumeric;
        public string Character;
        public Hex1bColor? Foreground;
        public Hex1bColor? Background;
        public double BgRevealTime;
        public double CharRevealTime;
        public double SettleTime;
        public int ScrambleSeed;
    }

    private CellInfo[,]? _cells;
    private int _width, _height;
    private readonly Random _rng = new();

    private const string ScrambleChars =
        "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdef@#$%&!?<>{}[]~";

    public void Reset()
    {
        _cells = null;
    }

    public void Update(int width, int height)
    {
        if (_cells is not null && _width == width && _height == height)
        {
            return;
        }

        _width = width;
        _height = height;
        _cells = new CellInfo[width, height];
    }

    public CellCompute GetCompute(double progress)
    {
        return ctx =>
        {
            var below = ctx.GetBelow();

            if (_cells is null)
            {
                return new SurfaceCell(" ", null, null);
            }

            ref var cell = ref _cells[ctx.X, ctx.Y];

            // Check if this cell has new content that wasn't there before
            bool hasVisibleChar = !below.IsContinuation
                && below.Character != "\uE000"
                && !string.IsNullOrEmpty(below.Character)
                && below.Character != " ";

            if (hasVisibleChar && !cell.HasContent)
            {
                // New content appeared — assign reveal times relative to current progress
                bool isAlpha = !IsStructuralChar(below.Character);
                double jitter = _rng.NextDouble() * 0.04;

                // Start revealing shortly after current progress
                double bgReveal = Math.Max(cell.BgRevealTime, progress + jitter);
                double charReveal = isAlpha
                    ? bgReveal + 0.08 + _rng.NextDouble() * 0.06
                    : bgReveal;
                double settle = isAlpha
                    ? charReveal + 0.15 + _rng.NextDouble() * 0.10
                    : charReveal;

                cell = new CellInfo
                {
                    HasContent = true,
                    IsAlphaNumeric = isAlpha,
                    Character = below.Character,
                    Foreground = below.Foreground,
                    Background = below.Background,
                    BgRevealTime = bgReveal,
                    CharRevealTime = charReveal,
                    SettleTime = settle,
                    ScrambleSeed = _rng.Next()
                };
            }
            else if (hasVisibleChar && cell.HasContent && cell.Character != below.Character)
            {
                // Content changed — update the target character but keep timing
                cell.Character = below.Character;
                cell.Foreground = below.Foreground;
                cell.Background = below.Background;
            }
            else if (!cell.Initialized)
            {
                // First time seeing this cell — set up background reveal timing
                double rowFrac = 1.0 - (double)ctx.Y / Math.Max(1, _height - 1);
                double jitter = _rng.NextDouble() * 0.06;

                cell = new CellInfo
                {
                    Initialized = true,
                    HasContent = false,
                    Background = below.Background,
                    BgRevealTime = rowFrac * 0.45 + jitter,
                };
            }

            if (progress >= 1.0)
            {
                return below;
            }

            if (progress < cell.BgRevealTime)
            {
                return new SurfaceCell(" ", null, Hex1bColor.FromRgb(0, 0, 0));
            }

            double bgFade = Math.Clamp((progress - cell.BgRevealTime) / 0.18, 0, 1);
            var bg = FadeFromBlack(cell.Background, bgFade);

            if (!cell.HasContent)
            {
                return new SurfaceCell(" ", null, bg);
            }

            if (progress < cell.CharRevealTime)
            {
                return new SurfaceCell(" ", null, bg);
            }

            double fgFade = Math.Clamp((progress - cell.CharRevealTime) / 0.08, 0, 1);
            var fg = FadeFromBlack(cell.Foreground, fgFade);

            if (!cell.IsAlphaNumeric)
            {
                return new SurfaceCell(cell.Character ?? " ", fg, bg) with
                {
                    Attributes = below.Attributes,
                    DisplayWidth = below.DisplayWidth
                };
            }

            if (progress >= cell.SettleTime)
            {
                return new SurfaceCell(cell.Character ?? " ", fg, bg) with
                {
                    Attributes = below.Attributes,
                    DisplayWidth = below.DisplayWidth
                };
            }

            int idx = (int)(progress * 200 + cell.ScrambleSeed) % ScrambleChars.Length;
            return new SurfaceCell(ScrambleChars[idx].ToString(), fg, bg);
        };
    }

    private static bool IsStructuralChar(string ch)
    {
        if (ch.Length == 0)
        {
            return false;
        }

        char c = ch[0];
        if (c is >= '\u2500' and <= '\u259F')
        {
            return true;
        }

        if (c is >= '\u2800' and <= '\u28FF')
        {
            return true;
        }

        if (c is '|' or '+' or '-' or '=' or '_')
        {
            return true;
        }

        return false;
    }

    private static Hex1bColor? FadeFromBlack(Hex1bColor? target, double amount)
    {
        if (target is null || target.Value.IsDefault)
        {
            return amount >= 1.0 ? target : Hex1bColor.FromRgb(0, 0, 0);
        }

        var c = target.Value;
        return Hex1bColor.FromRgb(
            (byte)(c.R * amount),
            (byte)(c.G * amount),
            (byte)(c.B * amount));
    }
}
