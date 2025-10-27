// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Data.Common;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Aspire.Hosting.OpenAI;

/// <summary>
/// A health check for OpenAI Model resources.
/// </summary>
/// <param name="httpClient">The HttpClient to use.</param>
/// <param name="connectionString">The connection string.</param>
internal sealed class OpenAIModelHealthCheck(HttpClient httpClient, Func<ValueTask<string?>> connectionString) : IHealthCheck
{
    private HealthCheckResult? _result;

    /// <summary>
    /// Checks the health of the OpenAI endpoint by sending a test request.
    /// </summary>
    /// <param name="context">The health check context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous health check operation.</returns>
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        if (_result is not null)
        {
            return _result.Value;
        }

        try
        {
            var builder = new DbConnectionStringBuilder() { ConnectionString = await connectionString().ConfigureAwait(false) };
            var endpoint = builder["Endpoint"];
            var model = builder["Model"];

            using var request = new HttpRequestMessage(HttpMethod.Get, new Uri($"{endpoint}/models/{model}"));

            // Add required headers
            request.Headers.Add("Authorization", $"Bearer {builder["Key"]}");

            using var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

            _result = response.StatusCode switch
            {
                HttpStatusCode.OK => HealthCheckResult.Healthy(),
                HttpStatusCode.Unauthorized => HealthCheckResult.Unhealthy("OpenAI API key is invalid"),
                HttpStatusCode.NotFound => await HandleNotFound(response, cancellationToken).ConfigureAwait(false),
                HttpStatusCode.TooManyRequests => HealthCheckResult.Unhealthy("OpenAI API rate limit exceeded"),
                _ => HealthCheckResult.Unhealthy($"OpenAI endpoint returned unexpected status code: {response.StatusCode}")
            };
        }
        catch (Exception ex)
        {
            _result = HealthCheckResult.Unhealthy($"Failed to check OpenAI endpoint: {ex.Message}", ex);
        }

        return _result.Value;
    }

    private static async Task<HealthCheckResult> HandleNotFound(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        OpenAIErrorResponse? errorResponse = null;

        try
        {
            var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            errorResponse = JsonSerializer.Deserialize<OpenAIErrorResponse>(content);

            if (errorResponse?.Error?.Code == "model_not_found")
            {
                var message = !string.IsNullOrEmpty(errorResponse.Error.Message)
                    ? errorResponse.Error.Message
                    : "Model not found";
                return HealthCheckResult.Unhealthy($"OpenAI: {message}");
            }
        }
        catch
        {
        }

        return HealthCheckResult.Unhealthy($"OpenAI returned an unsupported response: ({response.StatusCode}) {errorResponse?.Error?.Message}");
    }

    /// <summary>
    /// Represents the error response from OpenAI API.
    /// </summary>
    private sealed class OpenAIErrorResponse
    {
        [JsonPropertyName("error")]
        public OpenAIError? Error { get; set; }
    }

    /// <summary>
    /// Represents an error from OpenAI API.
    /// </summary>
    private sealed class OpenAIError
    {
        [JsonPropertyName("code")]
        public string? Code { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; }
    }
}
