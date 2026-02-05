// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;

namespace Aspire.Tools.Service;

internal static class ExceptionExtensions
{
    /// <summary>
    /// Given an exception, returns a string which has concatenated the ex.message and inner exception message
    ///  if it exits. If it is an aggregate exception it concatenates all the exceptions that are in the aggregate
    ///</summary> 
    public static string GetMessageFromException(this Exception ex)
    {
        string msg = string.Empty;
        if (ex is AggregateException aggException)
        {
            foreach (var e in aggException.Flatten().InnerExceptions)
            {
                if (msg == string.Empty)
                {
                    msg = e.Message;
                }
                else
                {
                    msg += " ";
                    msg += e.Message;
                }
            }
        }
        else
        {
            msg = ex.Message;
            if (ex.InnerException != null)
            {
                msg += " ";
                msg += ex.InnerException.Message;
            }
        }

        return msg;
    }
}
