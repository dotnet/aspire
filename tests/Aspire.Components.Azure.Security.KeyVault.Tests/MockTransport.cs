// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Text;
using Azure.Core.Pipeline;
using Azure.Core;
using Azure;

namespace Aspire.Azure;

public class MockTransport : HttpPipelineTransport
{
    private readonly object _syncObj = new object();
    private readonly Func<HttpMessage, MockResponse> _responseFactory;

    public List<MockRequest> Requests { get; } = new List<MockRequest>();

    public MockTransport(params MockResponse[] responses)
    {
        var requestIndex = 0;
        _responseFactory = _ =>
        {
            lock (_syncObj)
            {
                return responses[requestIndex++];
            }
        };
    }

    public MockTransport(Func<MockRequest, MockResponse> responseFactory)
    {
        _responseFactory = req => responseFactory((MockRequest)req.Request);
    }

    public override Request CreateRequest()
        => new MockRequest();

    public override void Process(HttpMessage message)
    {
        ProcessCore(message).GetAwaiter().GetResult();
    }

    public override async ValueTask ProcessAsync(HttpMessage message)
    {
        await ProcessCore(message);
    }

    private Task ProcessCore(HttpMessage message)
    {
        if (!(message.Request is MockRequest request))
        {
            throw new InvalidOperationException("the request is not compatible with the transport");
        }

        message.Response = null!;

        lock (_syncObj)
        {
            Requests.Add(request);
        }

        message.Response = _responseFactory(message);

        message.Response.ClientRequestId = request.ClientRequestId;

        return Task.CompletedTask;
    }
}

public class MockRequest : Request
{
    public MockRequest()
    {
        ClientRequestId = Guid.NewGuid().ToString();
    }

    private readonly Dictionary<string, List<string>> _headers = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
    public bool IsDisposed { get; private set; }

    protected override void SetHeader(string name, string value) => _headers[name] = [value];

    protected override void AddHeader(string name, string value)
    {
        AddHeader(new HttpHeader(name, value));
    }

    public void AddHeader(HttpHeader header)
    {
        if (!_headers.TryGetValue(header.Name, out var values))
        {
            _headers[header.Name] = values = new List<string>();
        }

        values.Add(header.Value);
    }

    protected override bool TryGetHeader(string name, [NotNullWhen(true)] out string? value)
    {
        if (_headers.TryGetValue(name, out var values))
        {
            value = JoinHeaderValue(values);
            return true;
        }

        value = null;
        return false;
    }

    protected override bool TryGetHeaderValues(string name, out IEnumerable<string> values) => throw new NotImplementedException();

    protected override bool ContainsHeader(string name) => _headers.TryGetValue(name, out _);

    protected override bool RemoveHeader(string name) => _headers.Remove(name);

    protected override IEnumerable<HttpHeader> EnumerateHeaders() => _headers.Select(h => new HttpHeader(h.Key, JoinHeaderValue(h.Value)));

    public override string ClientRequestId { get; set; }

    public override string ToString() => $"{Method} {Uri}";

    public override void Dispose()
    {
        IsDisposed = true;
    }
    private static string JoinHeaderValue(IEnumerable<string> values)
    {
        return string.Join(",", values);
    }
}

public class MockResponse : Response
{
    private readonly Dictionary<string, List<string>> _headers = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

    public MockResponse(int status, string? reasonPhrase = null)
    {
        Status = status;
        ReasonPhrase = reasonPhrase!;
    }

    public override int Status { get; }

    public override string ReasonPhrase { get; }

    public override Stream? ContentStream { get; set; }

    public override string ClientRequestId { get; set; } = string.Empty;

    private bool? _isError;
    public override bool IsError { get => _isError ?? base.IsError; }
    public void SetIsError(bool value) => _isError = value;

    public bool IsDisposed { get; private set; }

    public void SetContent(byte[] content)
    {
        ContentStream = new MemoryStream(content, 0, content.Length, false, true);
    }

    public MockResponse SetContent(string content)
    {
        SetContent(Encoding.UTF8.GetBytes(content));
        return this;
    }

    public MockResponse AddHeader(string name, string value)
    {
        return AddHeader(new HttpHeader(name, value));
    }

    public MockResponse AddHeader(HttpHeader header)
    {
        if (!_headers.TryGetValue(header.Name, out var values))
        {
            _headers[header.Name] = values = new List<string>();
        }

        values.Add(header.Value);
        return this;
    }

    protected override bool TryGetHeader(string name, [NotNullWhen(true)] out string? value)
    {
        if (_headers.TryGetValue(name, out var values))
        {
            value = JoinHeaderValue(values);
            return true;
        }

        value = null;
        return false;
    }

    protected override bool TryGetHeaderValues(string name, [NotNullWhen(true)] out IEnumerable<string>? values)
    {
        var result = _headers.TryGetValue(name, out var valuesList);
        values = valuesList;
        return result;
    }

    protected override bool ContainsHeader(string name)
    {
        return TryGetHeaderValues(name, out _);
    }

    protected override IEnumerable<HttpHeader> EnumerateHeaders() => _headers.Select(h => new HttpHeader(h.Key, JoinHeaderValue(h.Value)));

    private static string JoinHeaderValue(IEnumerable<string> values)
    {
        return string.Join(",", values);
    }

    public override void Dispose()
    {
        IsDisposed = true;
    }
}
