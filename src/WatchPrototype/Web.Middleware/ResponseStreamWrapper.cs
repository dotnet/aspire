// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.IO.Pipelines;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Watch.BrowserRefresh
{
    /// <summary>
    /// Wraps the Response Stream to inject the WebSocket HTML into
    /// an HTML Page.
    /// </summary>
    public class ResponseStreamWrapper : Stream
    {
        private static readonly MediaTypeHeaderValue s_textHtmlMediaType = new("text/html");

        private readonly HttpContext _context;
        private readonly ILogger _logger;
        private bool? _isHtmlResponse;

        private Stream _baseStream;
        private ScriptInjectingStream? _scriptInjectingStream;
        private Pipe? _pipe;
        private Task? _gzipCopyTask;
        private bool _disposed;

        public ResponseStreamWrapper(HttpContext context, ILogger logger)
        {
            _context = context;
            _baseStream = context.Response.Body;
            _logger = logger;
        }

        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length { get; }
        public override long Position { get; set; }
        public bool ScriptInjectionPerformed => _scriptInjectingStream?.ScriptInjectionPerformed == true;
        public bool IsHtmlResponse => _isHtmlResponse == true;

        public override void Flush()
        {
            OnWrite();
            _baseStream.Flush();
        }

        public override async Task FlushAsync(CancellationToken cancellationToken)
        {
            OnWrite();
            await _baseStream.FlushAsync(cancellationToken);
        }

        public override void Write(ReadOnlySpan<byte> buffer)
        {
            OnWrite();
            _baseStream.Write(buffer);
        }

        public override void WriteByte(byte value)
        {
            OnWrite();
            _baseStream.WriteByte(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            OnWrite();
            _baseStream.Write(buffer.AsSpan(offset, count));
        }

        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            OnWrite();
            await _baseStream.WriteAsync(buffer.AsMemory(offset, count), cancellationToken);
        }

        public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            OnWrite();
            await _baseStream.WriteAsync(buffer, cancellationToken);
        }

        private void OnWrite()
        {
            if (_isHtmlResponse.HasValue)
            {
                return;
            }

            var response = _context.Response;

            _isHtmlResponse =
                (response.StatusCode == StatusCodes.Status200OK || 
                 response.StatusCode == StatusCodes.Status404NotFound || 
                 response.StatusCode == StatusCodes.Status500InternalServerError) &&
                MediaTypeHeaderValue.TryParse(response.ContentType, out var mediaType) &&
                mediaType.IsSubsetOf(s_textHtmlMediaType) &&
                (!mediaType.Charset.HasValue || mediaType.Charset.Equals("utf-8", StringComparison.OrdinalIgnoreCase));

            if (!_isHtmlResponse.Value)
            {
                BrowserRefreshMiddleware.Log.ScriptInjectionSkipped(_logger, response.StatusCode, response.ContentType);
                return;
            }

            BrowserRefreshMiddleware.Log.SetupResponseForBrowserRefresh(_logger);
            // Since we're changing the markup content, reset the content-length
            response.Headers.ContentLength = null;

            _scriptInjectingStream = new ScriptInjectingStream(_baseStream);

            // By default, write directly to the script injection stream.
            // We may change the base stream below if we detect that the response
            // is compressed.
            _baseStream = _scriptInjectingStream;

            // Check if the response has gzip Content-Encoding
            if (response.Headers.TryGetValue(HeaderNames.ContentEncoding, out var contentEncodingValues))
            {
                var contentEncoding = contentEncodingValues.FirstOrDefault();
                if (string.Equals(contentEncoding, "gzip", StringComparison.OrdinalIgnoreCase))
                {
                    // Remove the Content-Encoding header since we'll be serving uncompressed content
                    response.Headers.Remove(HeaderNames.ContentEncoding);

                    _pipe = new Pipe();
                    var gzipStream = new GZipStream(_pipe.Reader.AsStream(leaveOpen: true), CompressionMode.Decompress, leaveOpen: true);

                    _gzipCopyTask = gzipStream.CopyToAsync(_scriptInjectingStream);
                    _baseStream = _pipe.Writer.AsStream(leaveOpen: true);
                }
            }
        }

        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
             => throw new NotSupportedException();

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
             => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                DisposeAsync().AsTask().GetAwaiter().GetResult();
            }
        }

        public ValueTask CompleteAsync() => DisposeAsync();

        public override async ValueTask DisposeAsync()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            if (_pipe is not null)
            {
                await _pipe.Writer.CompleteAsync();
            }

            if (_gzipCopyTask is not null)
            {
                await _gzipCopyTask;
            }

            if (_scriptInjectingStream is not null)
            {
                await _scriptInjectingStream.CompleteAsync();
            }
            else
            {
                Debug.Assert(_isHtmlResponse != true);
                await _baseStream.FlushAsync();
            }
        }
    }
}
