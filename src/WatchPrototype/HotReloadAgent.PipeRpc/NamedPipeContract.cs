// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.DotNet.HotReload;

internal interface IMessage
{
    ValueTask WriteAsync(Stream stream, CancellationToken cancellationToken);
}

internal interface IRequest : IMessage
{
    RequestType Type { get; }
}

internal interface IResponse : IMessage
{
    ResponseType Type { get; }
}

internal interface IUpdateRequest : IRequest
{
}

internal enum RequestType : byte
{
    ManagedCodeUpdate = 1,
    StaticAssetUpdate = 2,
    InitialUpdatesCompleted = 3,
}

internal enum ResponseType : byte
{
    InitializationResponse = 1,
    UpdateResponse = 2,
    HotReloadExceptionNotification = 3,
}

internal readonly struct ManagedCodeUpdateRequest(IReadOnlyList<RuntimeManagedCodeUpdate> updates, ResponseLoggingLevel responseLoggingLevel) : IUpdateRequest
{
    private const byte Version = 4;

    public IReadOnlyList<RuntimeManagedCodeUpdate> Updates { get; } = updates;
    public ResponseLoggingLevel ResponseLoggingLevel { get; } = responseLoggingLevel;
    public RequestType Type => RequestType.ManagedCodeUpdate;

    public async ValueTask WriteAsync(Stream stream, CancellationToken cancellationToken)
    {
        await stream.WriteAsync(Version, cancellationToken);
        await stream.WriteAsync(Updates.Count, cancellationToken);

        foreach (var update in Updates)
        {
            await stream.WriteAsync(update.ModuleId, cancellationToken);
            await stream.WriteByteArrayAsync(update.MetadataDelta, cancellationToken);
            await stream.WriteByteArrayAsync(update.ILDelta, cancellationToken);
            await stream.WriteByteArrayAsync(update.PdbDelta, cancellationToken);
            await stream.WriteAsync(update.UpdatedTypes, cancellationToken);
        }

        await stream.WriteAsync((byte)ResponseLoggingLevel, cancellationToken);
    }

    public static async ValueTask<ManagedCodeUpdateRequest> ReadAsync(Stream stream, CancellationToken cancellationToken)
    {
        var version = await stream.ReadByteAsync(cancellationToken);
        if (version != Version)
        {
            throw new NotSupportedException($"Unsupported version {version}.");
        }

        var count = await stream.ReadInt32Async(cancellationToken);

        var updates = new RuntimeManagedCodeUpdate[count];
        for (var i = 0; i < count; i++)
        {
            var moduleId = await stream.ReadGuidAsync(cancellationToken);
            var metadataDelta = await stream.ReadByteArrayAsync(cancellationToken);
            var ilDelta = await stream.ReadByteArrayAsync(cancellationToken);
            var pdbDelta = await stream.ReadByteArrayAsync(cancellationToken);
            var updatedTypes = await stream.ReadIntArrayAsync(cancellationToken);

            updates[i] = new RuntimeManagedCodeUpdate(moduleId, metadataDelta: metadataDelta, ilDelta: ilDelta, pdbDelta: pdbDelta, updatedTypes);
        }

        var responseLoggingLevel = (ResponseLoggingLevel)await stream.ReadByteAsync(cancellationToken);
        return new ManagedCodeUpdateRequest(updates, responseLoggingLevel: responseLoggingLevel);
    }
}

internal readonly struct UpdateResponse(IReadOnlyCollection<(string message, AgentMessageSeverity severity)> log, bool success) : IResponse
{
    public ResponseType Type => ResponseType.UpdateResponse;

    public async ValueTask WriteAsync(Stream stream, CancellationToken cancellationToken)
    {
        await stream.WriteAsync(success, cancellationToken);
        await stream.WriteAsync(log.Count, cancellationToken);

        foreach (var (message, severity) in log)
        {
            await stream.WriteAsync(message, cancellationToken);
            await stream.WriteAsync((byte)severity, cancellationToken);
        }
    }

    public static async ValueTask<(bool success, IAsyncEnumerable<(string message, AgentMessageSeverity severity)>)> ReadAsync(
        Stream stream, CancellationToken cancellationToken)
    {
        var success = await stream.ReadBooleanAsync(cancellationToken);
        var log = ReadLogAsync(cancellationToken);
        return (success, log);

        async IAsyncEnumerable<(string message, AgentMessageSeverity severity)> ReadLogAsync([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var entryCount = await stream.ReadInt32Async(cancellationToken);

            for (var i = 0; i < entryCount; i++)
            {
                var message = await stream.ReadStringAsync(cancellationToken);
                var severity = (AgentMessageSeverity)await stream.ReadByteAsync(cancellationToken);
                yield return (message, severity);
            }
        }
    }
}

internal readonly struct ClientInitializationResponse(string capabilities) : IResponse
{
    private const byte Version = 0;

    public ResponseType Type => ResponseType.InitializationResponse;

    public string Capabilities { get; } = capabilities;

    public async ValueTask WriteAsync(Stream stream, CancellationToken cancellationToken)
    {
        await stream.WriteAsync(Version, cancellationToken);
        await stream.WriteAsync(Capabilities, cancellationToken);
    }

    public static async ValueTask<ClientInitializationResponse> ReadAsync(Stream stream, CancellationToken cancellationToken)
    {
        var version = await stream.ReadByteAsync(cancellationToken);
        if (version != Version)
        {
            throw new NotSupportedException($"Unsupported version {version}.");
        }

        var capabilities = await stream.ReadStringAsync(cancellationToken);
        return new ClientInitializationResponse(capabilities);
    }
}

internal readonly struct HotReloadExceptionCreatedNotification(int code, string message) : IResponse
{
    public ResponseType Type => ResponseType.HotReloadExceptionNotification;
    public int Code => code;
    public string Message => message;

    public async ValueTask WriteAsync(Stream stream, CancellationToken cancellationToken)
    {
        await stream.WriteAsync(code, cancellationToken);
        await stream.WriteAsync(message, cancellationToken);
    }

    public static async ValueTask<HotReloadExceptionCreatedNotification> ReadAsync(Stream stream, CancellationToken cancellationToken)
    {
        var code = await stream.ReadInt32Async(cancellationToken);
        var message = await stream.ReadStringAsync(cancellationToken);
        return new HotReloadExceptionCreatedNotification(code, message);
    }
}

internal readonly struct StaticAssetUpdateRequest(
    RuntimeStaticAssetUpdate update,
    ResponseLoggingLevel responseLoggingLevel) : IUpdateRequest
{
    private const byte Version = 2;

    public RuntimeStaticAssetUpdate Update { get; } = update;
    public ResponseLoggingLevel ResponseLoggingLevel { get; } = responseLoggingLevel;

    public RequestType Type => RequestType.StaticAssetUpdate;

    public async ValueTask WriteAsync(Stream stream, CancellationToken cancellationToken)
    {
        await stream.WriteAsync(Version, cancellationToken);
        await stream.WriteAsync(Update.AssemblyName, cancellationToken);
        await stream.WriteAsync(Update.IsApplicationProject, cancellationToken);
        await stream.WriteAsync(Update.RelativePath, cancellationToken);
        await stream.WriteByteArrayAsync(Update.Contents, cancellationToken);
        await stream.WriteAsync((byte)ResponseLoggingLevel, cancellationToken);
    }

    public static async ValueTask<StaticAssetUpdateRequest> ReadAsync(Stream stream, CancellationToken cancellationToken)
    {
        var version = await stream.ReadByteAsync(cancellationToken);
        if (version != Version)
        {
            throw new NotSupportedException($"Unsupported version {version}.");
        }

        var assemblyName = await stream.ReadStringAsync(cancellationToken);
        var isApplicationProject = await stream.ReadBooleanAsync(cancellationToken);
        var relativePath = await stream.ReadStringAsync(cancellationToken);
        var contents = await stream.ReadByteArrayAsync(cancellationToken);
        var responseLoggingLevel = (ResponseLoggingLevel)await stream.ReadByteAsync(cancellationToken);

        return new StaticAssetUpdateRequest(
            new RuntimeStaticAssetUpdate(
                assemblyName: assemblyName,
                relativePath: relativePath,
                contents: contents,
                isApplicationProject),
            responseLoggingLevel);
    }
}
