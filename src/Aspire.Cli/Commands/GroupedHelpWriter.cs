// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;

namespace Aspire.Cli.Commands;

/// <summary>
/// Writes grouped help output for the root command, organizing subcommands into logical categories.
/// </summary>
internal static class GroupedHelpWriter
{
    /// <summary>
    /// A command entry within a group, with an optional usage override.
    /// </summary>
    /// <param name="Name">The command name.</param>
    /// <param name="UsageOverride">
    /// When set, replaces the auto-generated argument syntax for this entry.
    /// Use an empty string to suppress arguments entirely.
    /// </param>
    private sealed record CommandEntry(string Name, string? UsageOverride = null);

    private sealed record CommandGroup(string Heading, CommandEntry[] Commands);

    private static readonly CommandGroup[] s_groups =
    [
        new("App Commands:", [
            new("new"),
            new("init"),
            new("add"),
            new("update"),
            new("run"),
            new("stop", UsageOverride: ""),
            new("ps"),
        ]),
        new("Resource Management:", [
            new("start"),
            new("stop"),
            new("restart"),
            new("wait"),
            new("command"),
        ]),
        new("Monitoring:", [
            new("describe"),
            new("logs"),
            new("otel"),
        ]),
        new("Deployment:", [
            new("publish"),
            new("deploy"),
            new("do"),
        ]),
        new("Tools & Configuration:", [
            new("config"),
            new("cache"),
            new("doctor"),
            new("docs"),
            new("agent"),
            new("setup"),
        ]),
    ];

    /// <summary>
    /// Writes grouped help output for the given root command.
    /// </summary>
    public static void WriteHelp(Command command, TextWriter writer)
    {
        var maxWidth = GetConsoleWidth();

        // Description
        if (!string.IsNullOrEmpty(command.Description))
        {
            writer.WriteLine(command.Description);
            writer.WriteLine();
        }

        // Usage
        writer.WriteLine("Usage:");
        writer.WriteLine("  aspire <command> [options]");
        writer.WriteLine();

        // Build a lookup from command name to the Command object (visible subcommands only).
        var subcommandLookup = new Dictionary<string, Command>(StringComparer.OrdinalIgnoreCase);
        foreach (var sub in command.Subcommands)
        {
            if (!sub.Hidden)
            {
                subcommandLookup[sub.Name] = sub;
            }
        }

        // Compute the first-column width across all groups for consistent alignment.
        var columnWidth = 0;
        foreach (var group in s_groups)
        {
            foreach (var entry in group.Commands)
            {
                if (subcommandLookup.TryGetValue(entry.Name, out var cmd))
                {
                    var label = FormatCommandLabel(cmd, entry.UsageOverride);
                    if (label.Length > columnWidth)
                    {
                        columnWidth = label.Length;
                    }
                }
            }
        }

        // Padding: 2 spaces indent + label + at least 2 spaces gap before description
        columnWidth += 4;

        // Write each group.
        foreach (var group in s_groups)
        {
            var hasAny = false;

            foreach (var entry in group.Commands)
            {
                if (!subcommandLookup.TryGetValue(entry.Name, out var cmd))
                {
                    continue;
                }

                if (!hasAny)
                {
                    writer.WriteLine(group.Heading);
                    hasAny = true;
                }

                var label = FormatCommandLabel(cmd, entry.UsageOverride);
                var description = cmd.Description ?? string.Empty;
                WriteTwoColumnRow(writer, label, description, columnWidth, maxWidth);
            }

            if (hasAny)
            {
                writer.WriteLine();
            }
        }

        // Options
        var visibleOptions = command.Options.Where(o => !o.Hidden).ToList();
        if (visibleOptions.Count > 0)
        {
            writer.WriteLine("Options:");

            var optionColumnWidth = 0;
            foreach (var opt in visibleOptions)
            {
                var label = FormatOptionLabel(opt);
                if (label.Length > optionColumnWidth)
                {
                    optionColumnWidth = label.Length;
                }
            }

            optionColumnWidth += 4;

            foreach (var opt in visibleOptions)
            {
                var label = FormatOptionLabel(opt);
                var desc = opt.Description ?? string.Empty;
                WriteTwoColumnRow(writer, label, desc, optionColumnWidth, maxWidth);
            }

            writer.WriteLine();
        }

        // Help hint
        writer.WriteLine("Use \"aspire <command> --help\" for more information about a command.");
    }

    /// <summary>
    /// Writes a two-column row with word-wrapping on the description column.
    /// Continuation lines are indented to align with the description start.
    /// </summary>
    private static void WriteTwoColumnRow(TextWriter writer, string label, string description, int columnWidth, int maxWidth)
    {
        const int indent = 2;
        var paddedLabel = label.PadRight(columnWidth);
        var descriptionWidth = maxWidth - columnWidth - indent;

        // If the terminal is too narrow to wrap meaningfully, just write it all on one line.
        if (descriptionWidth < 20)
        {
            writer.WriteLine($"{new string(' ', indent)}{paddedLabel}{description}");
            return;
        }

        var remaining = description.AsSpan();
        var firstLine = true;

        while (remaining.Length > 0)
        {
            if (firstLine)
            {
                writer.Write(new string(' ', indent));
                writer.Write(paddedLabel);
                firstLine = false;
            }
            else
            {
                // Continuation line: indent to align with description column.
                writer.Write(new string(' ', indent + columnWidth));
            }

            if (remaining.Length <= descriptionWidth)
            {
                writer.WriteLine(remaining);
                break;
            }

            // Find the last space within the allowed width for a clean word break.
            var breakAt = remaining[..descriptionWidth].LastIndexOf(' ');
            if (breakAt <= 0)
            {
                // No space found — hard break at the width limit.
                breakAt = descriptionWidth;
            }

            writer.WriteLine(remaining[..breakAt]);
            remaining = remaining[breakAt..].TrimStart();
        }
    }

    private static string FormatCommandLabel(Command cmd, string? usageOverride)
    {
        var args = usageOverride ?? GetArgumentSyntax(cmd);
        return string.IsNullOrEmpty(args) ? cmd.Name : $"{cmd.Name} {args}";
    }

    private static string GetArgumentSyntax(Command cmd)
    {
        if (cmd.Arguments.Count == 0)
        {
            return string.Empty;
        }

        var parts = new List<string>();
        foreach (var arg in cmd.Arguments)
        {
            if (arg.Hidden)
            {
                continue;
            }

            var name = $"<{arg.Name}>";

            // Optional if minimum arity is 0.
            if (arg.Arity.MinimumNumberOfValues == 0)
            {
                name = $"[{name}]";
            }

            parts.Add(name);
        }

        return string.Join(" ", parts);
    }

    private static string FormatOptionLabel(Option option)
    {
        // Collect all identifiers: Name may not be in Aliases in System.CommandLine 2.0.
        var allNames = new HashSet<string>(option.Aliases, StringComparer.Ordinal);
        if (!string.IsNullOrEmpty(option.Name))
        {
            allNames.Add(option.Name);
        }

        var sorted = allNames.OrderBy(a => a.Length).ToList();
        return sorted.Count > 1
            ? $"{sorted[0]}, {sorted[1]}"
            : sorted.Count > 0 ? sorted[0] : option.Name;
    }

    private static int GetConsoleWidth()
    {
        try
        {
            var width = Console.WindowWidth;
            return width > 0 ? width : 80;
        }
        catch
        {
            return 80;
        }
    }
}
