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
        ]),
        new("Resource Management:", [
            new("run"),
            new("stop"),
            new("start"),
            new("restart"),
            new("command"),
            new("wait"),
            new("ps"),
            new("resources"),
            new("logs"),
            new("telemetry"),
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
        writer.WriteLine("  aspire [command] [options]");
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

                var label = FormatCommandLabel(cmd, entry.UsageOverride).PadRight(columnWidth);
                var description = cmd.Description ?? string.Empty;

                // Truncate description to fit within terminal width.
                var descriptionWidth = maxWidth - columnWidth - 2; // 2 for leading indent
                if (descriptionWidth > 20 && description.Length > descriptionWidth)
                {
                    description = description[..(descriptionWidth - 3)] + "...";
                }

                writer.WriteLine($"  {label}{description}");
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
                var label = FormatOptionLabel(opt).PadRight(optionColumnWidth);
                var desc = opt.Description ?? string.Empty;
                writer.WriteLine($"  {label}{desc}");
            }

            writer.WriteLine();
        }

        // Help hint
        writer.WriteLine("Use \"aspire [command] --help\" for more information about a command.");
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
        var aliases = option.Aliases.OrderBy(a => a.Length).ToList();
        if (aliases.Count > 1)
        {
            return $"{aliases[0]}, {aliases[1]}";
        }

        return aliases.Count > 0 ? aliases[0] : option.Name;
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
