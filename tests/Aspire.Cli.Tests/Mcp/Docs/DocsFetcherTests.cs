// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using Aspire.Cli.Mcp.Docs;
using Microsoft.Extensions.Logging.Abstractions;

namespace Aspire.Cli.Tests.Mcp.Docs;

public class DocsFetcherTests
{
    [Fact]
    public async Task FetchDocsAsync_SuccessfulRequest_ReturnsContent()
    {
        var expectedContent = """
            # Redis Integration
            > Connect to Redis.

            Content here.
            """;

        var handler = new MockHttpMessageHandler(new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(expectedContent)
        });

        var httpClient = new HttpClient(handler);
        var cache = new MockDocsCache();
        var fetcher = new DocsFetcher(httpClient, cache, NullLogger<DocsFetcher>.Instance);

        var content = await fetcher.FetchDocsAsync();

        Assert.Equal(expectedContent, content);
    }

    [Fact]
    public async Task FetchDocsAsync_StoresETag_WhenProvided()
    {
        var response = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent("# Content")
        };
        response.Headers.ETag = new System.Net.Http.Headers.EntityTagHeaderValue("\"abc123\"");

        var handler = new MockHttpMessageHandler(response);
        var httpClient = new HttpClient(handler);
        var cache = new MockDocsCache();
        var fetcher = new DocsFetcher(httpClient, cache, NullLogger<DocsFetcher>.Instance);

        await fetcher.FetchDocsAsync();

        var storedETag = await cache.GetETagAsync("https://aspire.dev/llms-small.txt");
        Assert.Equal("\"abc123\"", storedETag);
    }

    [Fact]
    public async Task FetchDocsAsync_CachesContent()
    {
        var content = "# Cached Content";
        var response = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(content)
        };

        var handler = new MockHttpMessageHandler(response);
        var httpClient = new HttpClient(handler);
        var cache = new MockDocsCache();
        var fetcher = new DocsFetcher(httpClient, cache, NullLogger<DocsFetcher>.Instance);

        await fetcher.FetchDocsAsync();

        var cached = await cache.GetAsync("https://aspire.dev/llms-small.txt");
        Assert.Equal(content, cached);
    }

    [Fact]
    public async Task FetchDocsAsync_WithCachedETag_SendsIfNoneMatchHeader()
    {
        var cache = new MockDocsCache();
        await cache.SetETagAsync("https://aspire.dev/llms-small.txt", "\"cached-etag\"");
        await cache.SetAsync("https://aspire.dev/llms-small.txt", "# Cached");

        var response = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.NotModified
        };

        var handler = new MockHttpMessageHandler(response, request =>
        {
            Assert.Contains("\"cached-etag\"", request.Headers.IfNoneMatch.ToString());
        });

        var httpClient = new HttpClient(handler);
        var fetcher = new DocsFetcher(httpClient, cache, NullLogger<DocsFetcher>.Instance);

        await fetcher.FetchDocsAsync();

        Assert.True(handler.RequestValidated);
    }

    [Fact]
    public async Task FetchDocsAsync_NotModifiedResponse_ReturnsCachedContent()
    {
        var cachedContent = "# Cached Content";
        var cache = new MockDocsCache();
        await cache.SetETagAsync("https://aspire.dev/llms-small.txt", "\"etag\"");
        await cache.SetAsync("https://aspire.dev/llms-small.txt", cachedContent);

        var response = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.NotModified
        };

        var handler = new MockHttpMessageHandler(response);
        var httpClient = new HttpClient(handler);
        var fetcher = new DocsFetcher(httpClient, cache, NullLogger<DocsFetcher>.Instance);

        var content = await fetcher.FetchDocsAsync();

        Assert.Equal(cachedContent, content);
    }

    [Fact]
    public async Task FetchDocsAsync_NotModifiedButCacheEmpty_RetriesWithoutETag()
    {
        var freshContent = "# Fresh Content";
        var cache = new MockDocsCache();
        await cache.SetETagAsync("https://aspire.dev/llms-small.txt", "\"etag\"");
        // Cache content is empty - simulating cache cleared but ETag remains

        var callCount = 0;
        var handler = new MockHttpMessageHandler(_ =>
        {
            callCount++;
            if (callCount == 1)
            {
                // First call returns NotModified
                return new HttpResponseMessage { StatusCode = HttpStatusCode.NotModified };
            }
            // Retry returns fresh content
            var response = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(freshContent)
            };
            response.Headers.ETag = new System.Net.Http.Headers.EntityTagHeaderValue("\"new-etag\"");
            return response;
        });

        var httpClient = new HttpClient(handler);
        var fetcher = new DocsFetcher(httpClient, cache, NullLogger<DocsFetcher>.Instance);

        var content = await fetcher.FetchDocsAsync();

        Assert.Equal(freshContent, content);
        Assert.Equal(2, callCount);
    }

    [Fact]
    public async Task FetchDocsAsync_FailedRequest_ReturnsNull()
    {
        var response = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.InternalServerError
        };

        var handler = new MockHttpMessageHandler(response);
        var httpClient = new HttpClient(handler);
        var cache = new MockDocsCache();
        var fetcher = new DocsFetcher(httpClient, cache, NullLogger<DocsFetcher>.Instance);

        var content = await fetcher.FetchDocsAsync();

        Assert.Null(content);
    }

    [Fact]
    public async Task FetchDocsAsync_NetworkError_ReturnsNull()
    {
        var handler = new MockHttpMessageHandler(new HttpRequestException("Network error"));
        var httpClient = new HttpClient(handler);
        var cache = new MockDocsCache();
        var fetcher = new DocsFetcher(httpClient, cache, NullLogger<DocsFetcher>.Instance);

        var content = await fetcher.FetchDocsAsync();

        Assert.Null(content);
    }

    [Fact]
    public async Task FetchDocsAsync_Cancellation_ReturnsNull()
    {
        // Use a handler that properly checks the cancellation token
        var handler = new CancellationCheckingHandler();
        var httpClient = new HttpClient(handler);
        var cache = new MockDocsCache();
        var fetcher = new DocsFetcher(httpClient, cache, NullLogger<DocsFetcher>.Instance);

        var cts = new CancellationTokenSource();
        cts.Cancel();

        // The FetchDocsAsync swallows exceptions and returns null when there's no cached content
        var result = await fetcher.FetchDocsAsync(cts.Token);
        Assert.Null(result);
    }

    [Fact]
    public async Task FetchDocsAsync_EmptyResponse_ReturnsEmptyString()
    {
        var handler = new MockHttpMessageHandler(new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent("")
        });

        var httpClient = new HttpClient(handler);
        var cache = new MockDocsCache();
        var fetcher = new DocsFetcher(httpClient, cache, NullLogger<DocsFetcher>.Instance);

        var content = await fetcher.FetchDocsAsync();

        Assert.Equal("", content);
    }

    [Fact]
    public async Task FetchDocsAsync_WhitespaceOnlyResponse_ReturnsWhitespace()
    {
        var whitespace = "   \n\t\n   ";
        var handler = new MockHttpMessageHandler(new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(whitespace)
        });

        var httpClient = new HttpClient(handler);
        var cache = new MockDocsCache();
        var fetcher = new DocsFetcher(httpClient, cache, NullLogger<DocsFetcher>.Instance);

        var content = await fetcher.FetchDocsAsync();

        Assert.Equal(whitespace, content);
    }

    [Fact]
    public async Task FetchDocsAsync_NullContent_ReturnsEmptyString()
    {
        var handler = new MockHttpMessageHandler(new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = null
        });

        var httpClient = new HttpClient(handler);
        var cache = new MockDocsCache();
        var fetcher = new DocsFetcher(httpClient, cache, NullLogger<DocsFetcher>.Instance);

        var content = await fetcher.FetchDocsAsync();

        // When Content is null, ReadAsStringAsync returns empty string
        Assert.Equal("", content);
    }

    [Fact]
    public async Task FetchDocsAsync_NotFoundResponse_ReturnsNull()
    {
        var handler = new MockHttpMessageHandler(new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.NotFound
        });

        var httpClient = new HttpClient(handler);
        var cache = new MockDocsCache();
        var fetcher = new DocsFetcher(httpClient, cache, NullLogger<DocsFetcher>.Instance);

        var content = await fetcher.FetchDocsAsync();

        Assert.Null(content);
    }

    [Fact]
    public async Task FetchDocsAsync_BadRequestResponse_ReturnsNull()
    {
        var handler = new MockHttpMessageHandler(new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.BadRequest
        });

        var httpClient = new HttpClient(handler);
        var cache = new MockDocsCache();
        var fetcher = new DocsFetcher(httpClient, cache, NullLogger<DocsFetcher>.Instance);

        var content = await fetcher.FetchDocsAsync();

        Assert.Null(content);
    }

    [Fact]
    public async Task FetchDocsAsync_ServiceUnavailableResponse_ReturnsNull()
    {
        var handler = new MockHttpMessageHandler(new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.ServiceUnavailable
        });

        var httpClient = new HttpClient(handler);
        var cache = new MockDocsCache();
        var fetcher = new DocsFetcher(httpClient, cache, NullLogger<DocsFetcher>.Instance);

        var content = await fetcher.FetchDocsAsync();

        Assert.Null(content);
    }

    [Fact]
    public async Task FetchDocsAsync_GatewayTimeoutResponse_ReturnsNull()
    {
        var handler = new MockHttpMessageHandler(new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.GatewayTimeout
        });

        var httpClient = new HttpClient(handler);
        var cache = new MockDocsCache();
        var fetcher = new DocsFetcher(httpClient, cache, NullLogger<DocsFetcher>.Instance);

        var content = await fetcher.FetchDocsAsync();

        Assert.Null(content);
    }

    [Fact]
    public async Task FetchDocsAsync_TimeoutException_ReturnsNull()
    {
        var handler = new MockHttpMessageHandler(new TaskCanceledException("Request timed out"));
        var httpClient = new HttpClient(handler);
        var cache = new MockDocsCache();
        var fetcher = new DocsFetcher(httpClient, cache, NullLogger<DocsFetcher>.Instance);

        var content = await fetcher.FetchDocsAsync();

        Assert.Null(content);
    }

    [Fact]
    public async Task FetchDocsAsync_OperationCanceledException_ReturnsNull()
    {
        var handler = new MockHttpMessageHandler(new OperationCanceledException("Operation was cancelled"));
        var httpClient = new HttpClient(handler);
        var cache = new MockDocsCache();
        var fetcher = new DocsFetcher(httpClient, cache, NullLogger<DocsFetcher>.Instance);

        var content = await fetcher.FetchDocsAsync();

        Assert.Null(content);
    }

    [Fact]
    public async Task FetchDocsAsync_InvalidOperationException_ReturnsNull()
    {
        var handler = new MockHttpMessageHandler(new InvalidOperationException("Invalid state"));
        var httpClient = new HttpClient(handler);
        var cache = new MockDocsCache();
        var fetcher = new DocsFetcher(httpClient, cache, NullLogger<DocsFetcher>.Instance);

        var content = await fetcher.FetchDocsAsync();

        Assert.Null(content);
    }

    [Fact]
    public async Task FetchDocsAsync_CacheReturnsNull_FetchesFromServer()
    {
        var serverContent = "# Server Content";
        var handler = new MockHttpMessageHandler(new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(serverContent)
        });

        var httpClient = new HttpClient(handler);
        var cache = new MockDocsCache();
        // Cache is empty, no ETag
        var fetcher = new DocsFetcher(httpClient, cache, NullLogger<DocsFetcher>.Instance);

        var content = await fetcher.FetchDocsAsync();

        Assert.Equal(serverContent, content);
    }

    [Fact]
    public async Task FetchDocsAsync_WithETagButNoCachedContent_ClearsETagAndRetries()
    {
        var serverContent = "# Fresh Content";
        var cache = new MockDocsCache();
        await cache.SetETagAsync("https://aspire.dev/llms-small.txt", "\"old-etag\"");
        // Note: no cached content set

        var callCount = 0;
        var handler = new MockHttpMessageHandler(_ =>
        {
            callCount++;
            if (callCount == 1)
            {
                return new HttpResponseMessage { StatusCode = HttpStatusCode.NotModified };
            }
            return new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(serverContent)
            };
        });

        var httpClient = new HttpClient(handler);
        var fetcher = new DocsFetcher(httpClient, cache, NullLogger<DocsFetcher>.Instance);

        var content = await fetcher.FetchDocsAsync();

        Assert.Equal(serverContent, content);
        Assert.Equal(2, callCount);
    }

    private sealed class CancellationCheckingHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("# Content")
            });
        }
    }

    private sealed class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage>? _responseFactory;
        private readonly HttpResponseMessage? _response;
        private readonly Exception? _exception;
        private readonly Action<HttpRequestMessage>? _requestValidator;

        public bool RequestValidated { get; private set; }

        public MockHttpMessageHandler(HttpResponseMessage response, Action<HttpRequestMessage>? requestValidator = null)
        {
            _response = response;
            _requestValidator = requestValidator;
        }

        public MockHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
        {
            _responseFactory = responseFactory;
        }

        public MockHttpMessageHandler(Exception exception)
        {
            _exception = exception;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (_exception is not null)
            {
                throw _exception;
            }

            if (_requestValidator is not null)
            {
                _requestValidator(request);
                RequestValidated = true;
            }

            if (_responseFactory is not null)
            {
                return Task.FromResult(_responseFactory(request));
            }

            return Task.FromResult(_response!);
        }
    }

    private sealed class MockDocsCache : IDocsCache
    {
        private readonly Dictionary<string, string> _content = [];
        private readonly Dictionary<string, string> _etags = [];

        public Task<string?> GetAsync(string key, CancellationToken cancellationToken = default)
        {
            _content.TryGetValue(key, out var value);
            return Task.FromResult(value);
        }

        public Task SetAsync(string key, string content, CancellationToken cancellationToken = default)
        {
            _content[key] = content;
            return Task.CompletedTask;
        }

        public Task<string?> GetETagAsync(string url, CancellationToken cancellationToken = default)
        {
            _etags.TryGetValue(url, out var value);
            return Task.FromResult(value);
        }

        public Task SetETagAsync(string url, string? etag, CancellationToken cancellationToken = default)
        {
            if (etag is null)
            {
                _etags.Remove(url);
            }
            else
            {
                _etags[url] = etag;
            }
            return Task.CompletedTask;
        }

        public Task InvalidateAsync(string key, CancellationToken cancellationToken = default)
        {
            _content.Remove(key);
            return Task.CompletedTask;
        }
    }
}
