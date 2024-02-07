// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Aspire.Dashboard.Otlp.Model;
using Aspire.Dashboard.Resources;
using Microsoft.JSInterop;

namespace Aspire.Dashboard.Components;

public partial class LogMessageColumnDisplay
{
    private bool TryGetErrorInformation([NotNullWhen(true)] out string? errorInfo)
    {
        var stackTrace = GetProperty("exception.stacktrace") ?? GetProperty("ex.StackTrace");
        if (stackTrace is null)
        {
            errorInfo = null;
            return false;
        }

        if (string.IsNullOrEmpty(stackTrace))
        {
            var message = GetProperty("exception.message") ?? GetProperty("ex.Message");
            var type = GetProperty("exception.type") ?? GetProperty("ex.Type");
            errorInfo = $"{type}: {message}";
        }
        else
        {
            errorInfo = stackTrace;
        }

        return true;

        string? GetProperty(string propertyName)
        {
            return LogEntry.Properties.GetValue(propertyName);
        }
    }

    private async Task CopyTextToClipboardAsync(string? text, string id)
        => await JS.InvokeVoidAsync("copyTextToClipboard", id, text, ControlsStringsLoc[nameof(ControlsStrings.GridValueCopyToClipboard)].ToString(), ControlsStringsLoc[nameof(ControlsStrings.GridValueCopied)].ToString());

}
