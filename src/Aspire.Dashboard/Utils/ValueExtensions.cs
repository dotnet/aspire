// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Google.Protobuf.WellKnownTypes;

namespace Aspire.Dashboard.Utils;

internal static class ValueExtensions
{
    public static bool TryConvertToInt(this Value value, out int i)
    {
        if (value.HasStringValue && int.TryParse(value.StringValue, CultureInfo.InvariantCulture, out i))
        {
            return true;
        }
        else if (value.HasNumberValue)
        {
            i = (int)Math.Round(value.NumberValue);
            return true;
        }

        i = 0;
        return false;
    }

    public static bool TryConvertToString(this Value value, [NotNullWhen(returnValue: true)] out string? s)
    {
        if (value.HasStringValue)
        {
            s = value.StringValue;
            return true;
        }

        s = null;
        return false;
    }
}
