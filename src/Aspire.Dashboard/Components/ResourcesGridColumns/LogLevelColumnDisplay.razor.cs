// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Aspire.Dashboard.Otlp.Model;
using Aspire.Dashboard.Resources;
using Microsoft.JSInterop;

namespace Aspire.Dashboard.Components;

public partial class LogLevelColumnDisplay
{
    private bool TryGetErrorInformation([MaybeNullWhen(false)] out string errorInfo)
    {
        if (LogEntry.Properties.GetValue("ex.Type") is { } exceptionType)
        {
            var message = LogEntry.Properties.GetValue("ex.Message");
            var stackTrace = LogEntry.Properties.GetValue("ex.StackTrace");
            errorInfo = LogEntry.Message + Environment.NewLine + $"{exceptionType}: {message}" + Environment.NewLine + stackTrace;

            Debug.Assert(message is not null);
            Debug.Assert(stackTrace is not null);

            return true;
        }

        errorInfo = null;
        return false;
    }

    private async Task CopyTextToClipboardAsync(string? text, string id)
        => await JS.InvokeVoidAsync("copyTextToClipboard", id, text, ControlsStringsLoc[nameof(ControlsStrings.GridValueCopyToClipboard)].ToString(), ControlsStringsLoc[nameof(ControlsStrings.GridValueCopied)].ToString());
}
