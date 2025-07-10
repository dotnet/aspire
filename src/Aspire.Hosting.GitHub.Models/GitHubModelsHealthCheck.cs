// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Data.Common;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Aspire.Hosting.GitHub.Models;

/// <summary>
/// A health check for GitHub Models resources.
/// </summary>
/// <param name="httpClient">The HttpClient to use.</param>
/// <param name="connectionString">The connection string.</param>
internal sealed class GitHubModelsHealthCheck(HttpClient httpClient, Func<ValueTask<string?>> connectionString) : IHealthCheck
{
    /// <summary>
    /// Checks the health of the GitHub Models endpoint by sending a test request.
    /// </summary>
    /// <param name="context">The health check context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous health check operation.</returns>
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var builder = new DbConnectionStringBuilder() { ConnectionString = await connectionString().ConfigureAwait(false) };

            using var request = new HttpRequestMessage(HttpMethod.Post, new Uri($"{builder["Endpoint"]}/chat/completions"));

            // Add required headers
            request.Headers.Add("Accept", "application/vnd.github+json");
            request.Headers.Add("Authorization", $"Bearer {builder["Key"]?.ToString()}");
            request.Headers.Add("X-GitHub-Api-Version", "2022-11-28");
            
            // Create test payload with empty messages to minimize API usage
            var payload = new
            {
                model = builder["Model"]?.ToString(),
                messages = Array.Empty<object>()
            };
            
            var jsonPayload = JsonSerializer.Serialize(payload);
            request.Content = new StringContent(jsonPayload, System.Text.Encoding.UTF8, "application/json");
            
            using var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            
            return response.StatusCode switch
            {
                HttpStatusCode.Unauthorized => HealthCheckResult.Unhealthy("GitHub Models API key is invalid or has insufficient permissions"),
                HttpStatusCode.NotFound or HttpStatusCode.Forbidden or HttpStatusCode.BadRequest => await HandleErrorCode(response, cancellationToken).ConfigureAwait(false),
                _ => HealthCheckResult.Unhealthy($"GitHub Models endpoint returned unexpected status code: {response.StatusCode}")
            };
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy($"Failed to check GitHub Models endpoint: {ex.Message}", ex);
        }
    }

    private static async Task<HealthCheckResult> HandleErrorCode(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        GitHubErrorResponse? errorResponse = null;
        
        try
        {
            var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            errorResponse = JsonSerializer.Deserialize<GitHubErrorResponse>(content);

            if (errorResponse?.Error?.Code == "unknown_model")
            {
                var message = !string.IsNullOrEmpty(errorResponse.Error.Message)
                    ? errorResponse.Error.Message
                    : "Unknown model";
                return HealthCheckResult.Unhealthy($"GitHub Models: {message}");
            }
            else if (errorResponse?.Error?.Code == "empty_array")
            {
                return HealthCheckResult.Healthy();
            }
            else if (errorResponse?.Error?.Code == "no_access")
            {
                return HealthCheckResult.Unhealthy($"GitHub Models: {errorResponse.Error.Message}");
            }
        }
        catch
        {
        }

        return HealthCheckResult.Unhealthy($"GitHub Models returned an unsupported resonse: ({response.StatusCode}) {errorResponse?.Error?.Message}");
    }

    /// <summary>
    /// Represents the error response from GitHub Models API.
    /// </summary>
    private sealed class GitHubErrorResponse
    {
        [JsonPropertyName("error")]
        public GitHubError? Error { get; set; }
    }

    /// <summary>
    /// Represents an error from GitHub Models API.
    /// </summary>
    private sealed class GitHubError
    {
        [JsonPropertyName("code")]
        public string? Code { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }

        [JsonPropertyName("details")]
        public string? Details { get; set; }
    }
}
