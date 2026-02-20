// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;

namespace Aspire.Cli.Commands;

/// <summary>
/// Writes grouped help output for the root command, organizing subcommands into logical categories.
/// </summary>
internal static class GroupedHelpWriter
{
    private sealed record CommandGroup(string Heading, string[] CommandNames);

    private static readonly CommandGroup[] s_groups =
    [
        new("App Commands:", ["new", "init", "add", "update", "run", "stop", "start"]),
        new("Resource Management:", ["run", "stop", "start", "restart", "command", "wait", "ps", "resources", "logs", "telemetry"]),
        new("Deployment:", ["publish", "deploy", "do"]),
        new("Tools & Configuration:", ["config", "cache", "doctor", "docs", "agent", "setup"]),
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

        // Compute the column width once across all groups for consistent alignment.
        var columnWidth = 0;
        foreach (var group in s_groups)
        {
            foreach (var name in group.CommandNames)
            {
                if (subcommandLookup.ContainsKey(name) && name.Length > columnWidth)
                {
                    columnWidth = name.Length;
                }
            }
        }

        // Padding: 2 spaces indent + name + at least 2 spaces gap
        columnWidth += 4;

        // Write each group.
        foreach (var group in s_groups)
        {
            var hasAny = false;

            foreach (var name in group.CommandNames)
            {
                if (!subcommandLookup.ContainsKey(name))
                {
                    continue;
                }

                if (!hasAny)
                {
                    writer.WriteLine(group.Heading);
                    hasAny = true;
                }

                var cmd = subcommandLookup[name];
                var paddedName = cmd.Name.PadRight(columnWidth);
                var description = cmd.Description ?? string.Empty;

                // Wrap description to fit within terminal width.
                var descriptionWidth = maxWidth - columnWidth - 2; // 2 for leading indent
                if (descriptionWidth > 20 && description.Length > descriptionWidth)
                {
                    description = description[..(descriptionWidth - 3)] + "...";
                }

                writer.WriteLine($"  {paddedName}{description}");
            }

            if (hasAny)
            {
                writer.WriteLine();
            }
        }

        // Options
        var options = command.Options;
        if (options.Count > 0)
        {
            var visibleOptions = options.Where(o => !o.Hidden).ToList();
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
        }

        // Help hint
        writer.WriteLine("Use \"aspire [command] --help\" for more information about a command.");
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
