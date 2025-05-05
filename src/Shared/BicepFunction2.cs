// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.RegularExpressions;
using Azure.Provisioning.Expressions;

namespace Azure.Provisioning;

internal static partial class BicepFunction2
{
    public static BicepValue<string> Interpolate(BicepFormatString formatString)
    {
        return Interpolate(formatString.Format, formatString.Args);
    }

    // See https://github.com/Azure/azure-sdk-for-net/issues/47360
    public static BicepValue<string> Interpolate(string format, object[] args)
    {
        // Flatten simple format strings
        if (format is "{0}" && args is [object arg])
        {
            return arg switch
            {
                BicepValue<string> bicepValue => bicepValue,
                string s => s,
                ProvisioningParameter provisioningParameter => provisioningParameter,
                BicepFormatString nested => Interpolate(nested.Format, nested.Args),
                _ => throw new NotSupportedException($"{args[0]} is not supported")
            };
        }

        var bicepStringBuilder = new BicepStringBuilder();
        var argumentIndex = 0;

        void ProcessFormatString(string format, object[] args, int argumentIndex)
        {
            var span = format.AsSpan();
            var skip = 0;

            foreach (var match in PlaceholderRegex().EnumerateMatches(span))
            {
                bicepStringBuilder.Append(span[..(match.Index - skip)].ToString());

                var argument = args[argumentIndex];

                if (argument is BicepValue<string> bicepValue)
                {
                    bicepStringBuilder.Append($"{bicepValue}");
                }
                else if (argument is string s)
                {
                    bicepStringBuilder.Append(s);
                }
                else if (argument is ProvisioningParameter provisioningParameter)
                {
                    bicepStringBuilder.Append($"{provisioningParameter}");
                }
                else if (argument is BicepFormatString nested)
                {
                    ProcessFormatString(nested.Format, nested.Args, 0);
                }
                else
                {
                    throw new NotSupportedException($"{argument} is not supported");
                }

                argumentIndex++;
                span = span[(match.Index + match.Length - skip)..];
                skip = match.Index + match.Length;
            }

            bicepStringBuilder.Append(span.ToString());
        }

        ProcessFormatString(format, args, argumentIndex);

        return bicepStringBuilder.Build();
    }

    [GeneratedRegex(@"{\d+}")]
    private static partial Regex PlaceholderRegex();
}

internal sealed class BicepFormatString(string format, object[] args)
{
    public string Format { get; } = format;
    public object[] Args { get; } = args;
}