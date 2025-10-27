// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Net;

namespace Aspire.Dashboard.Model.Assistant;

public static class OtelAttributeHelpers
{
    public static string GetHttpStatusName(int statusCode)
    {
        if (Enum.IsDefined(typeof(HttpStatusCode), statusCode))
        {
            return ((HttpStatusCode)statusCode).ToString();
        }
        return statusCode.ToString(CultureInfo.InvariantCulture);
    }

    public static string GetGrpcStatusName(int statusCode)
    {
        return statusCode switch
        {
            0 => "OK",
            1 => "CANCELLED",
            2 => "UNKNOWN",
            3 => "INVALID_ARGUMENT",
            4 => "DEADLINE_EXCEEDED",
            5 => "NOT_FOUND",
            6 => "ALREADY_EXISTS",
            7 => "PERMISSION_DENIED",
            8 => "RESOURCE_EXHAUSTED",
            9 => "FAILED_PRECONDITION",
            10 => "ABORTED",
            11 => "OUT_OF_RANGE",
            12 => "UNIMPLEMENTED",
            13 => "INTERNAL",
            14 => "UNAVAILABLE",
            15 => "DATA_LOSS",
            16 => "UNAUTHENTICATED",
            _ => statusCode.ToString(CultureInfo.InvariantCulture)
        };
    }
}
