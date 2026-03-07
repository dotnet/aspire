// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json.Nodes;
using Aspire.Hosting.RemoteHost.Ats;

namespace Aspire.Hosting.RemoteHost;

internal sealed class AtsSessionProxy : IAsyncDisposable
{
    private readonly object _session;
    private readonly MethodInfo _invokeCapabilityAsyncMethod;
    private readonly MethodInfo _cancelTokenMethod;

    public AtsSessionProxy(object session)
    {
        _session = session;
        var sessionType = session.GetType();

        _invokeCapabilityAsyncMethod = sessionType.GetMethod(
            "InvokeCapabilityAsync",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
            binder: null,
            types: [typeof(string), typeof(JsonObject)],
            modifiers: null)
            ?? throw new InvalidOperationException("AtsSession.InvokeCapabilityAsync was not found.");

        _cancelTokenMethod = sessionType.GetMethod(
            "CancelToken",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
            binder: null,
            types: [typeof(string)],
            modifiers: null)
            ?? throw new InvalidOperationException("AtsSession.CancelToken was not found.");
    }

    public async Task<JsonNode?> InvokeCapabilityAsync(string capabilityId, JsonObject? args)
    {
        try
        {
            var task = (Task<JsonNode?>?)_invokeCapabilityAsyncMethod.Invoke(_session, [capabilityId, args])
                ?? throw new InvalidOperationException("AtsSession.InvokeCapabilityAsync returned null.");

            return await task.ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            throw TranslateException(ex);
        }
    }

    public bool CancelToken(string tokenId)
    {
        try
        {
            return (bool?)_cancelTokenMethod.Invoke(_session, [tokenId])
                ?? throw new InvalidOperationException("AtsSession.CancelToken returned null.");
        }
        catch (Exception ex)
        {
            throw TranslateException(ex);
        }
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (_session is IAsyncDisposable asyncDisposable)
            {
                await asyncDisposable.DisposeAsync().ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            throw TranslateException(ex);
        }
    }

    private static Exception TranslateException(Exception exception)
    {
        var unwrapped = Unwrap(exception);
        if (TryCreateCapabilityException(unwrapped, out var capabilityException))
        {
            return capabilityException;
        }

        return unwrapped;
    }

    private static Exception Unwrap(Exception exception)
    {
        while (exception is TargetInvocationException { InnerException: { } inner })
        {
            exception = inner;
        }

        return exception;
    }

    private static bool TryCreateCapabilityException(Exception exception, [NotNullWhen(true)] out CapabilityException? capabilityException)
    {
        capabilityException = null;

        if (exception.GetType().FullName != "Aspire.Hosting.Ats.CapabilityException")
        {
            return false;
        }

        var errorProperty = exception.GetType().GetProperty("Error", BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException("Aspire.Hosting.Ats.CapabilityException.Error was not found.");

        var error = errorProperty.GetValue(exception)
            ?? throw new InvalidOperationException("Aspire.Hosting.Ats.CapabilityException.Error returned null.");

        capabilityException = new CapabilityException(MapError(error), exception);
        return true;
    }

    private static AtsError MapError(object error)
    {
        var errorType = error.GetType();

        return new AtsError
        {
            Code = (string?)errorType.GetProperty("Code", BindingFlags.Public | BindingFlags.Instance)?.GetValue(error)
                ?? throw new InvalidOperationException("Aspire.Hosting.Ats.AtsError.Code was not found."),
            Message = (string?)errorType.GetProperty("Message", BindingFlags.Public | BindingFlags.Instance)?.GetValue(error)
                ?? throw new InvalidOperationException("Aspire.Hosting.Ats.AtsError.Message was not found."),
            Capability = (string?)errorType.GetProperty("Capability", BindingFlags.Public | BindingFlags.Instance)?.GetValue(error),
            Details = MapErrorDetails(errorType.GetProperty("Details", BindingFlags.Public | BindingFlags.Instance)?.GetValue(error))
        };
    }

    private static AtsErrorDetails? MapErrorDetails(object? details)
    {
        if (details is null)
        {
            return null;
        }

        var detailsType = details.GetType();

        return new AtsErrorDetails
        {
            Parameter = (string?)detailsType.GetProperty("Parameter", BindingFlags.Public | BindingFlags.Instance)?.GetValue(details),
            Expected = (string?)detailsType.GetProperty("Expected", BindingFlags.Public | BindingFlags.Instance)?.GetValue(details),
            Actual = (string?)detailsType.GetProperty("Actual", BindingFlags.Public | BindingFlags.Instance)?.GetValue(details)
        };
    }
}
