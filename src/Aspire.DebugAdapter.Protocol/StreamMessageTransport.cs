// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Aspire.DebugAdapter.Types;

namespace Aspire.DebugAdapter.Protocol;

/// <summary>
/// A message transport that reads and writes DAP messages over streams using the standard
/// Content-Length header framing format.
/// </summary>
/// <remarks>
/// The DAP wire format is:
/// <code>
/// Content-Length: &lt;length&gt;\r\n
/// \r\n
/// &lt;JSON content&gt;
/// </code>
/// </remarks>
public sealed class StreamMessageTransport : IMessageTransport
{
    /// <summary>
    /// Default maximum message size (4 MB).
    /// </summary>
    public const int DefaultMaxMessageSize = 4 * 1024 * 1024;

    /// <summary>
    /// Default maximum header size (8 KB).
    /// </summary>
    public const int DefaultMaxHeaderSize = 8 * 1024;

    private const string ContentLengthHeader = "Content-Length: ";
    private const string HeaderTerminator = "\r\n\r\n";

    private readonly Stream _input;
    private readonly Stream _output;
    private readonly JsonTypeInfo<ProtocolMessage> _protocolMessageTypeInfo;
    private readonly JsonSerializerContext _jsonContext;
    private readonly SemaphoreSlim _writeLock = new(1, 1);
    private readonly byte[] _readBuffer = new byte[4096];
    private readonly int _maxMessageSize;
    private readonly int _maxHeaderSize;
    private int _readBufferOffset;
    private int _readBufferLength;
    private bool _disposed;

    /// <summary>
    /// Creates a new stream transport for DAP communication using the default JSON context.
    /// </summary>
    /// <param name="input">The input stream to read messages from.</param>
    /// <param name="output">The output stream to write messages to.</param>
    /// <param name="maxMessageSize">Maximum allowed message size in bytes. Defaults to <see cref="DefaultMaxMessageSize"/>.</param>
    /// <param name="maxHeaderSize">Maximum allowed header size in bytes. Defaults to <see cref="DefaultMaxHeaderSize"/>.</param>
    public StreamMessageTransport(
        Stream input,
        Stream output,
        int maxMessageSize = DefaultMaxMessageSize,
        int maxHeaderSize = DefaultMaxHeaderSize)
        : this(input, output, DefaultDebugAdapterJsonContext.Default, maxMessageSize, maxHeaderSize)
    {
    }

    /// <summary>
    /// Creates a new stream transport for DAP communication with a custom JSON context.
    /// </summary>
    /// <param name="input">The input stream to read messages from.</param>
    /// <param name="output">The output stream to write messages to.</param>
    /// <param name="jsonContext">
    /// JSON serializer context containing type info for DAP types. Use this overload when you need
    /// custom serialization settings or have extended the DAP types.
    /// </param>
    /// <param name="maxMessageSize">Maximum allowed message size in bytes. Defaults to <see cref="DefaultMaxMessageSize"/>.</param>
    /// <param name="maxHeaderSize">Maximum allowed header size in bytes. Defaults to <see cref="DefaultMaxHeaderSize"/>.</param>
    public StreamMessageTransport(
        Stream input,
        Stream output,
        JsonSerializerContext jsonContext,
        int maxMessageSize = DefaultMaxMessageSize,
        int maxHeaderSize = DefaultMaxHeaderSize)
    {
        _input = input ?? throw new ArgumentNullException(nameof(input));
        _output = output ?? throw new ArgumentNullException(nameof(output));
        _jsonContext = jsonContext ?? throw new ArgumentNullException(nameof(jsonContext));
        _protocolMessageTypeInfo = (JsonTypeInfo<ProtocolMessage>)jsonContext.GetTypeInfo(typeof(ProtocolMessage))!;

        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(maxMessageSize, 0);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(maxHeaderSize, 0);

        _maxMessageSize = maxMessageSize;
        _maxHeaderSize = maxHeaderSize;
    }

    /// <summary>
    /// Creates a transport using standard input/output streams with the default JSON context.
    /// </summary>
    /// <param name="maxMessageSize">Maximum allowed message size in bytes. Defaults to <see cref="DefaultMaxMessageSize"/>.</param>
    /// <param name="maxHeaderSize">Maximum allowed header size in bytes. Defaults to <see cref="DefaultMaxHeaderSize"/>.</param>
    public static StreamMessageTransport CreateStdio(
        int maxMessageSize = DefaultMaxMessageSize,
        int maxHeaderSize = DefaultMaxHeaderSize)
    {
        return new StreamMessageTransport(
            Console.OpenStandardInput(),
            Console.OpenStandardOutput(),
            maxMessageSize,
            maxHeaderSize);
    }

    /// <summary>
    /// Creates a transport using standard input/output streams with a custom JSON context.
    /// </summary>
    /// <param name="jsonContext">
    /// JSON serializer context containing type info for DAP types. Use this overload when you need
    /// custom serialization settings or have extended the DAP types.
    /// </param>
    /// <param name="maxMessageSize">Maximum allowed message size in bytes. Defaults to <see cref="DefaultMaxMessageSize"/>.</param>
    /// <param name="maxHeaderSize">Maximum allowed header size in bytes. Defaults to <see cref="DefaultMaxHeaderSize"/>.</param>
    public static StreamMessageTransport CreateStdio(
        JsonSerializerContext jsonContext,
        int maxMessageSize = DefaultMaxMessageSize,
        int maxHeaderSize = DefaultMaxHeaderSize)
    {
        return new StreamMessageTransport(
            Console.OpenStandardInput(),
            Console.OpenStandardOutput(),
            jsonContext,
            maxMessageSize,
            maxHeaderSize);
    }

    /// <inheritdoc />
    public async Task SendAsync(ProtocolMessage message, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        // Serialize at the appropriate intermediate level to get nested polymorphism discriminators.
        // System.Text.Json only writes one level of discriminator, so we need to serialize as
        // RequestMessage/ResponseMessage/EventMessage to get both "type" and "command"/"event".
        var json = SerializeWithNestedPolymorphism(message);
        var header = $"{ContentLengthHeader}{json.Length}\r\n\r\n";
        var headerBytes = Encoding.ASCII.GetBytes(header);

        await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await _output.WriteAsync(headerBytes, cancellationToken).ConfigureAwait(false);
            await _output.WriteAsync(json, cancellationToken).ConfigureAwait(false);
            await _output.FlushAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    /// <summary>
    /// Serializes a DAP message with support for nested polymorphism.
    /// </summary>
    private byte[] SerializeWithNestedPolymorphism(ProtocolMessage message)
    {
        // Serialize using the intermediate type to get both levels of discriminators written.
        // When serializing as RequestMessage, we get both "type":"request" from ProtocolMessage
        // polymorphism AND "command":"xyz" from RequestMessage polymorphism.
        return message switch
        {
            RequestMessage req => JsonSerializer.SerializeToUtf8Bytes(req, _jsonContext.GetTypeInfo(typeof(RequestMessage))!),
            ResponseMessage resp => JsonSerializer.SerializeToUtf8Bytes(resp, _jsonContext.GetTypeInfo(typeof(ResponseMessage))!),
            EventMessage evt => JsonSerializer.SerializeToUtf8Bytes(evt, _jsonContext.GetTypeInfo(typeof(EventMessage))!),
            _ => JsonSerializer.SerializeToUtf8Bytes(message, _protocolMessageTypeInfo)
        };
    }

    /// <inheritdoc />
    public async Task<ProtocolMessage?> ReceiveAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        // Read headers until we find Content-Length
        var contentLength = await ReadContentLengthAsync(cancellationToken).ConfigureAwait(false);
        if (contentLength < 0)
        {
            return null; // Stream closed
        }

        // Read the JSON content
        var content = new byte[contentLength];
        var bytesRead = 0;

        // First, consume any data remaining in our buffer
        if (_readBufferLength > 0)
        {
            var toCopy = Math.Min(_readBufferLength, contentLength);
            Array.Copy(_readBuffer, _readBufferOffset, content, 0, toCopy);
            bytesRead = toCopy;
            _readBufferOffset += toCopy;
            _readBufferLength -= toCopy;
        }

        // Read remaining content directly from stream
        while (bytesRead < contentLength)
        {
            var read = await _input.ReadAsync(content.AsMemory(bytesRead, contentLength - bytesRead), cancellationToken).ConfigureAwait(false);
            if (read == 0)
            {
                throw new DebugAdapterProtocolException("Connection closed while reading message content");
            }
            bytesRead += read;
        }

        try
        {
            // Two-pass deserialization to support nested polymorphism:
            // System.Text.Json only applies one level of polymorphism, so we need to:
            // 1. Peek at the "type" property to determine request/response/event
            // 2. Deserialize as the specific intermediate type which applies the second level
            return DeserializeWithNestedPolymorphism(content);
        }
        catch (JsonException ex)
        {
            throw new DebugAdapterProtocolException("Invalid JSON content in message", ex);
        }
    }

    /// <summary>
    /// Deserializes a DAP message with support for nested polymorphism.
    /// </summary>
    private ProtocolMessage? DeserializeWithNestedPolymorphism(byte[] content)
    {
        // Parse the JSON to peek at the "type" property
        using var doc = JsonDocument.Parse(content);
        var root = doc.RootElement;

        if (!root.TryGetProperty("type", out var typeProperty))
        {
            // No type property, fall back to base deserialization
            return JsonSerializer.Deserialize(content, _protocolMessageTypeInfo);
        }

        var messageType = typeProperty.GetString();

        // Deserialize using the appropriate intermediate type which will apply second-level polymorphism
        return messageType switch
        {
            "request" => (ProtocolMessage?)JsonSerializer.Deserialize(content, _jsonContext.GetTypeInfo(typeof(RequestMessage))!),
            "response" => (ProtocolMessage?)JsonSerializer.Deserialize(content, _jsonContext.GetTypeInfo(typeof(ResponseMessage))!),
            "event" => (ProtocolMessage?)JsonSerializer.Deserialize(content, _jsonContext.GetTypeInfo(typeof(EventMessage))!),
            _ => JsonSerializer.Deserialize(content, _protocolMessageTypeInfo)
        };
    }

    private async Task<int> ReadContentLengthAsync(CancellationToken cancellationToken)
    {
        var headerBuilder = new StringBuilder();

        while (true)
        {
            // Check header size limit to prevent memory exhaustion
            if (headerBuilder.Length > _maxHeaderSize)
            {
                throw new DebugAdapterProtocolException($"Header size exceeds maximum of {_maxHeaderSize} bytes");
            }

            // Try to find header terminator in current buffer
            var headerEndIndex = FindHeaderTerminator(headerBuilder);
            if (headerEndIndex >= 0)
            {
                var headers = headerBuilder.ToString(0, headerEndIndex);
                headerBuilder.Remove(0, headerEndIndex + HeaderTerminator.Length);

                // Put remaining data back into buffer for content reading
                if (headerBuilder.Length > 0)
                {
                    var remaining = Encoding.UTF8.GetBytes(headerBuilder.ToString());
                    Array.Copy(remaining, 0, _readBuffer, 0, remaining.Length);
                    _readBufferOffset = 0;
                    _readBufferLength = remaining.Length;
                }

                return ParseContentLength(headers);
            }

            // Need more data - read into buffer
            if (_readBufferLength == 0)
            {
                _readBufferOffset = 0;
                var read = await _input.ReadAsync(_readBuffer, cancellationToken).ConfigureAwait(false);
                if (read == 0)
                {
                    return -1; // Stream closed cleanly
                }
                _readBufferLength = read;
            }

            // Append buffer to header builder
            headerBuilder.Append(Encoding.UTF8.GetString(_readBuffer, _readBufferOffset, _readBufferLength));
            _readBufferLength = 0;
        }
    }

    private static int FindHeaderTerminator(StringBuilder sb)
    {
        for (int i = 0; i <= sb.Length - HeaderTerminator.Length; i++)
        {
            bool found = true;
            for (int j = 0; j < HeaderTerminator.Length; j++)
            {
                if (sb[i + j] != HeaderTerminator[j])
                {
                    found = false;
                    break;
                }
            }
            if (found)
            {
                return i;
            }
        }
        return -1;
    }

    private int ParseContentLength(string headers)
    {
        foreach (var line in headers.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith(ContentLengthHeader, StringComparison.OrdinalIgnoreCase))
            {
                var value = trimmed[ContentLengthHeader.Length..];
                if (int.TryParse(value, out var length))
                {
                    if (length <= 0)
                    {
                        throw new DebugAdapterProtocolException($"Content-Length must be positive, got: {length}");
                    }

                    if (length > _maxMessageSize)
                    {
                        throw new DebugAdapterProtocolException($"Content-Length {length} exceeds maximum allowed size of {_maxMessageSize} bytes");
                    }

                    return length;
                }

                throw new DebugAdapterProtocolException($"Invalid Content-Length value: {value}");
            }
        }

        throw new DebugAdapterProtocolException("Missing Content-Length header");
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        await _output.FlushAsync().ConfigureAwait(false);

        _writeLock.Dispose();

        // Don't dispose the streams - we don't own them
    }
}
