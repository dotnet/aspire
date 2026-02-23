// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;

namespace Aspire.Cli.Commands;

/// <summary>
/// Writes grouped help output for the root command, organizing subcommands into logical categories.
/// Groups are determined by each command's <see cref="BaseCommand.HelpGroup"/> property.
/// </summary>
internal static class GroupedHelpWriter
{
    /// <summary>
    /// The well-known group ordering. Groups not listed here appear after these, in alphabetical order.
    /// </summary>
    private static readonly string[] s_groupOrder =
    [
        HelpGroups.AppCommands,
        HelpGroups.ResourceManagement,
        HelpGroups.Monitoring,
        HelpGroups.Deployment,
        HelpGroups.ToolsAndConfiguration,
    ];

    /// <summary>
    /// Writes grouped help output for the given root command.
    /// </summary>
    /// <param name="command">The root command to generate help for.</param>
    /// <param name="writer">The text writer to write help output to.</param>
    /// <param name="maxWidth">The maximum console width. When null, defaults to 80.</param>
    public static void WriteHelp(Command command, TextWriter writer, int? maxWidth = null)
    {
        var width = maxWidth ?? 80;

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

        // Collect visible subcommands and organize by group.
        var grouped = new Dictionary<string, List<(BaseCommand Cmd, int Order)>>(StringComparer.Ordinal);
        var ungroupedCommands = new List<Command>();

        foreach (var sub in command.Subcommands)
        {
            if (sub.Hidden)
            {
                continue;
            }

            if (sub is BaseCommand baseCmd && baseCmd.HelpGroup is not null)
            {
                if (!grouped.TryGetValue(baseCmd.HelpGroup, out var list))
                {
                    list = [];
                    grouped[baseCmd.HelpGroup] = list;
                }

                list.Add((baseCmd, baseCmd.HelpGroupOrder));
            }
            else
            {
                ungroupedCommands.Add(sub);
            }
        }

        // Sort commands within each group by order.
        foreach (var list in grouped.Values)
        {
            list.Sort((a, b) => a.Order.CompareTo(b.Order));
        }

        // Compute the first-column width across all commands for consistent alignment.
        var columnWidth = 0;
        foreach (var list in grouped.Values)
        {
            foreach (var (cmd, _) in list)
            {
                var label = FormatCommandLabel(cmd);
                if (label.Length > columnWidth)
                {
                    columnWidth = label.Length;
                }
            }
        }

        foreach (var cmd in ungroupedCommands)
        {
            var label = FormatCommandLabel(cmd);
            if (label.Length > columnWidth)
            {
                columnWidth = label.Length;
            }
        }

        // Padding: 2 spaces indent + label + at least 2 spaces gap before description
        columnWidth += 4;

        // Write groups in the defined order, then any additional groups alphabetically.
        var writtenGroups = new HashSet<string>(StringComparer.Ordinal);

        foreach (var groupName in s_groupOrder)
        {
            if (grouped.TryGetValue(groupName, out var commands))
            {
                WriteGroup(writer, groupName, commands, columnWidth, width);
                writtenGroups.Add(groupName);
            }
        }

        // Write any groups not in the well-known order (future-proofing).
        foreach (var (groupName, commands) in grouped.OrderBy(kvp => kvp.Key, StringComparer.Ordinal))
        {
            if (!writtenGroups.Contains(groupName))
            {
                WriteGroup(writer, groupName, commands, columnWidth, width);
            }
        }

        // Catch-all: show any registered commands not assigned to a group.
        if (ungroupedCommands.Count > 0)
        {
            writer.WriteLine("Other Commands:");
            foreach (var cmd in ungroupedCommands.OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase))
            {
                var label = FormatCommandLabel(cmd);
                var description = cmd.Description ?? string.Empty;
                WriteTwoColumnRow(writer, label, description, columnWidth, width);
            }
            writer.WriteLine();
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
                WriteTwoColumnRow(writer, label, desc, optionColumnWidth, width);
            }

            writer.WriteLine();
        }

        // Help hint
        writer.WriteLine("Use \"aspire <command> --help\" for more information about a command.");
    }

    private static void WriteGroup(TextWriter writer, string heading, List<(BaseCommand Cmd, int Order)> commands, int columnWidth, int width)
    {
        writer.WriteLine(heading);
        foreach (var (cmd, _) in commands)
        {
            var label = FormatCommandLabel(cmd);
            var description = cmd.Description ?? string.Empty;
            WriteTwoColumnRow(writer, label, description, columnWidth, width);
        }
        writer.WriteLine();
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

    private static string FormatCommandLabel(Command cmd)
    {
        var args = GetArgumentSyntax(cmd);
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
}
