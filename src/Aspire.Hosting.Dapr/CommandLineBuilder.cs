// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Text;

namespace Aspire.Hosting.Dapr;

internal delegate IEnumerable<string> CommandLineArgBuilder();

internal sealed record CommandLine(string FileName, IEnumerable<string> Arguments)
{
    public string ArgumentString
    {
        get
        {
            StringBuilder builder = new();

            var args = this.Arguments.ToList();

            for (int i = 0; i < args.Count; i++)
            {
                var arg = args[i];

                if (arg is not null)
                {
                    if (i > 0)
                    {
                        builder.Append(' ');
                    }

                    builder.Append(arg);
                }
            }

            return builder.ToString();
        }
    }
}

internal static class CommandLineBuilder
{
    public static CommandLine Create(string fileName, params CommandLineArgBuilder[] argBuilders)
    {
        return new CommandLine(fileName, argBuilders.SelectMany(builder => builder()));
    }
}

internal static class CommandLineArgs
{
    public static CommandLineArgBuilder Args(params string[] args)
    {
        return Args((IEnumerable<string>)args);
    }

    public static CommandLineArgBuilder Args(IEnumerable<string>? args)
    {
        return () => (args ?? Enumerable.Empty<string>());
    }

    public static CommandLineArgBuilder Command(params string[] commands)
    {
        return () => commands;
    }

    public static CommandLineArgBuilder Command(CommandLine commandLine)
    {
        return Command(new[] { commandLine.FileName, commandLine.ArgumentString });
    }

    public static CommandLineArgBuilder Flag(string name)
    {
        return Flag(name, true);
    }

    public static CommandLineArgBuilder Flag(string name, bool? value)
    {
        return () => value == true ? new[] { name } : Enumerable.Empty<string>();
    }

    public static CommandLineArgBuilder NamedArg<T>(string name, T value, bool assignValue = false) where T : struct
    {
        return () =>
        {
            string? stringValue = Convert.ToString(value, CultureInfo.InvariantCulture);

            return stringValue is not null
                ? NamedStringArg(name, stringValue, assignValue)()
                : Enumerable.Empty<string>();
        };
    }

    public static CommandLineArgBuilder NamedArg<T>(string name, T? value, bool assignValue = false) where T : struct
    {
        return () =>
            value.HasValue
                ? NamedArg(name, value.Value, assignValue)()
                : Enumerable.Empty<string>();
    }

    public static CommandLineArgBuilder NamedArg(string name, string? value, bool assignValue = false)
    {
        return () =>
        {
            return value is not null
                ? NamedStringArg(name, value, assignValue)()
                : Enumerable.Empty<string>();
        };
    }

    public static CommandLineArgBuilder NamedArg(string name, IEnumerable<string>? values, bool assignValue = false)
    {
        return () =>
        {
            return (values ?? Enumerable.Empty<string>()).SelectMany(value => NamedArg(name, value, assignValue)());
        };
    }

    private static readonly char[] s_reservedChars = new[] { ' ', '&', '|', '(', ')', '<', '>', '^' };

    private static CommandLineArgBuilder NamedStringArg(string name, string value, bool assignValue)
    {
        bool hasReservedChars = value.Any(c => s_reservedChars.Contains(c)) == true;

        return () =>
        {
            return assignValue
                ? new[] { $"\"{name}={value}\"" }
                : new[] { name, hasReservedChars ? $"\"{value}\"" : value };
        };
    }

    public static CommandLineArgBuilder PostOptionsArgs(params CommandLineArgBuilder[] args)
    {
        return PostOptionsArgs(null, args);
    }

    public static CommandLineArgBuilder PostOptionsArgs(string? separator, params CommandLineArgBuilder[] args)
    {
        return PostOptionsArgs(separator, (IEnumerable<CommandLineArgBuilder>)args);
    }

    public static CommandLineArgBuilder PostOptionsArgs(string? separator, IEnumerable<CommandLineArgBuilder> args)
    {
        IEnumerable<string> GeneratePostOptionsArgs()
        {
            bool postOptions = false;

            foreach (var arg in args.SelectMany(builder => builder()))
            {
                if (!postOptions)
                {
                    postOptions = true;

                    yield return separator ?? "--";
                }

                yield return arg;
            }
        }

        return GeneratePostOptionsArgs;
    }
}
